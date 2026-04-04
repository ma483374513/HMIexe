using System.Collections.ObjectModel;
using System.Text;
using HMIexe.Core.Models.Alarm;
using HMIexe.Core.Services;
using HMIexe.Runtime.Utilities;

namespace HMIexe.Runtime.Services;

/// <summary>
/// 报警服务，实现 <see cref="IAlarmService"/> 接口。
/// 负责管理运行时报警的完整生命周期：触发、确认、清除，以及历史记录的导出。
/// 活动报警使用 <see cref="ObservableCollection{T}"/> 存储，支持 UI 数据绑定实时刷新。
/// </summary>
public class AlarmService : IAlarmService
{
    /// <summary>当前活动（未清除）的报警记录集合，支持 UI 绑定。</summary>
    private readonly ObservableCollection<HmiAlarmRecord> _activeAlarms = new();

    /// <summary>历史报警记录列表，包含所有已触发的报警（含已清除的）。</summary>
    private readonly List<HmiAlarmRecord> _alarmHistory = new();

    /// <summary>获取当前活动报警的只读列表。</summary>
    public IReadOnlyList<HmiAlarmRecord> ActiveAlarms => _activeAlarms;

    /// <summary>获取完整报警历史的只读列表。</summary>
    public IReadOnlyList<HmiAlarmRecord> AlarmHistory => _alarmHistory;

    /// <summary>当新报警被触发时引发，订阅方可用于通知或记录。</summary>
    public event EventHandler<HmiAlarmRecord>? AlarmRaised;

    /// <summary>当报警被确认时引发，订阅方可用于 UI 刷新或审计。</summary>
    public event EventHandler<HmiAlarmRecord>? AlarmAcknowledged;

    /// <summary>
    /// 根据报警定义触发一条新报警，同时写入活动列表和历史列表，并发布 <see cref="AlarmRaised"/> 事件。
    /// </summary>
    /// <param name="definition">触发报警所依据的报警定义。</param>
    public void RaiseAlarm(HmiAlarmDefinition definition)
    {
        var record = new HmiAlarmRecord
        {
            AlarmDefinitionId = definition.Id,
            Message = definition.Message,
            Severity = definition.Severity,
            IsActive = true
        };
        _activeAlarms.Add(record);
        _alarmHistory.Add(record);
        AlarmRaised?.Invoke(this, record);
    }

    /// <summary>
    /// 确认指定报警，记录确认时间和操作人，并发布 <see cref="AlarmAcknowledged"/> 事件。
    /// 若报警 ID 不存在于活动列表中，则忽略此调用。
    /// </summary>
    /// <param name="alarmId">要确认的报警记录 ID。</param>
    /// <param name="acknowledgedBy">执行确认操作的用户名。</param>
    public void AcknowledgeAlarm(string alarmId, string acknowledgedBy)
    {
        var alarm = _activeAlarms.FirstOrDefault(a => a.Id == alarmId);
        if (alarm != null)
        {
            alarm.AcknowledgedAt = DateTime.Now;
            alarm.AcknowledgedBy = acknowledgedBy;
            AlarmAcknowledged?.Invoke(this, alarm);
        }
    }

    /// <summary>
    /// 清除（关闭）指定活动报警：将 <see cref="HmiAlarmRecord.IsActive"/> 设为 <c>false</c>
    /// 并从活动列表中移除。历史记录中的对应条目保留不变。
    /// </summary>
    /// <param name="alarmId">要清除的报警记录 ID。</param>
    public void ClearAlarm(string alarmId)
    {
        var alarm = _activeAlarms.FirstOrDefault(a => a.Id == alarmId);
        if (alarm != null)
        {
            alarm.IsActive = false;
            _activeAlarms.Remove(alarm);
        }
    }

    /// <summary>
    /// 将完整的报警历史异步导出为 CSV 文件（UTF-8 编码）。
    /// CSV 列顺序：Id、AlarmDefinitionId、Message、Severity、OccurredAt、AcknowledgedAt、AcknowledgedBy、IsActive。
    /// </summary>
    /// <param name="filePath">目标文件路径。</param>
    public async Task ExportAlarmHistoryAsync(string filePath)
    {
        var sb = new StringBuilder();
        // 写入 CSV 表头
        sb.AppendLine("Id,AlarmDefinitionId,Message,Severity,OccurredAt,AcknowledgedAt,AcknowledgedBy,IsActive");
        foreach (var record in _alarmHistory)
        {
            // 每个字段均通过 CsvHelper.QuoteField 处理，确保含逗号或引号的内容被正确转义
            sb.AppendLine(string.Join(",",
                CsvHelper.QuoteField(record.Id),
                CsvHelper.QuoteField(record.AlarmDefinitionId),
                CsvHelper.QuoteField(record.Message),
                CsvHelper.QuoteField(record.Severity.ToString()),
                CsvHelper.QuoteField(record.OccurredAt.ToString("yyyy-MM-dd HH:mm:ss")),
                CsvHelper.QuoteField(record.AcknowledgedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty),
                CsvHelper.QuoteField(record.AcknowledgedBy),
                CsvHelper.QuoteField(record.IsActive.ToString())));
        }
        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
    }
}
