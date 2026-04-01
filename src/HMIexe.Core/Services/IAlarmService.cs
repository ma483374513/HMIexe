using HMIexe.Core.Models.Alarm;

namespace HMIexe.Core.Services;

public interface IAlarmService
{
    IReadOnlyList<HmiAlarmRecord> ActiveAlarms { get; }
    IReadOnlyList<HmiAlarmRecord> AlarmHistory { get; }
    void RaiseAlarm(HmiAlarmDefinition definition);
    void AcknowledgeAlarm(string alarmId, string acknowledgedBy);
    void ClearAlarm(string alarmId);
    Task ExportAlarmHistoryAsync(string filePath);
    event EventHandler<HmiAlarmRecord> AlarmRaised;
    event EventHandler<HmiAlarmRecord> AlarmAcknowledged;
}
