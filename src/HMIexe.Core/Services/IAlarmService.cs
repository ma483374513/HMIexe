using HMIexe.Core.Models.Alarm;

/// <summary>
/// 报警服务接口定义。
/// 提供报警的触发、确认、清除、历史导出及事件通知等能力，
/// 是运行时报警管理子系统的核心契约。
/// </summary>
namespace HMIexe.Core.Services;

/// <summary>
/// HMI 报警服务接口，管理运行时报警的完整生命周期。
/// 消费者可订阅 <see cref="AlarmRaised"/> 和 <see cref="AlarmAcknowledged"/> 事件以响应报警状态变化。
/// </summary>
public interface IAlarmService
{
    /// <summary>当前所有活动（未清除）报警的只读列表。</summary>
    IReadOnlyList<HmiAlarmRecord> ActiveAlarms { get; }

    /// <summary>历史报警记录的只读列表，包含已清除和已确认的报警。</summary>
    IReadOnlyList<HmiAlarmRecord> AlarmHistory { get; }

    /// <summary>
    /// 根据报警定义触发一条新报警，创建对应的 <see cref="HmiAlarmRecord"/> 并加入活动列表。
    /// </summary>
    /// <param name="definition">触发报警所依据的报警定义配置。</param>
    void RaiseAlarm(HmiAlarmDefinition definition);

    /// <summary>
    /// 确认指定报警，记录确认人和确认时间。
    /// </summary>
    /// <param name="alarmId">要确认的报警记录 ID。</param>
    /// <param name="acknowledgedBy">执行确认操作的操作员名称或标识。</param>
    void AcknowledgeAlarm(string alarmId, string acknowledgedBy);

    /// <summary>
    /// 清除（解除）指定报警，将其从活动列表移至历史记录。
    /// </summary>
    /// <param name="alarmId">要清除的报警记录 ID。</param>
    void ClearAlarm(string alarmId);

    /// <summary>
    /// 异步将报警历史记录导出为文件（如 CSV 或 Excel 格式）。
    /// </summary>
    /// <param name="filePath">导出文件的目标路径。</param>
    Task ExportAlarmHistoryAsync(string filePath);

    /// <summary>当有新报警被触发时引发的事件，参数为新创建的报警记录。</summary>
    event EventHandler<HmiAlarmRecord> AlarmRaised;

    /// <summary>当报警被操作员确认时引发的事件，参数为已确认的报警记录。</summary>
    event EventHandler<HmiAlarmRecord> AlarmAcknowledged;
}
