using System.Collections.ObjectModel;
using System.Text;
using HMIexe.Core.Models.Alarm;
using HMIexe.Core.Services;
using HMIexe.Runtime.Utilities;

namespace HMIexe.Runtime.Services;

public class AlarmService : IAlarmService
{
    private readonly ObservableCollection<HmiAlarmRecord> _activeAlarms = new();
    private readonly List<HmiAlarmRecord> _alarmHistory = new();

    public IReadOnlyList<HmiAlarmRecord> ActiveAlarms => _activeAlarms;
    public IReadOnlyList<HmiAlarmRecord> AlarmHistory => _alarmHistory;

    public event EventHandler<HmiAlarmRecord>? AlarmRaised;
    public event EventHandler<HmiAlarmRecord>? AlarmAcknowledged;

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

    public void ClearAlarm(string alarmId)
    {
        var alarm = _activeAlarms.FirstOrDefault(a => a.Id == alarmId);
        if (alarm != null)
        {
            alarm.IsActive = false;
            _activeAlarms.Remove(alarm);
        }
    }

    public async Task ExportAlarmHistoryAsync(string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,AlarmDefinitionId,Message,Severity,OccurredAt,AcknowledgedAt,AcknowledgedBy,IsActive");
        foreach (var record in _alarmHistory)
        {
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
