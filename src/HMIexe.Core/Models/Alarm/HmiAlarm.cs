namespace HMIexe.Core.Models.Alarm;

public enum AlarmSeverity { Info, Warning, Error, Critical }

public class HmiAlarmDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public AlarmSeverity Severity { get; set; } = AlarmSeverity.Warning;
    public string Message { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public string AcknowledgeScript { get; set; } = string.Empty;
}

public class HmiAlarmRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AlarmDefinitionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public AlarmSeverity Severity { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.Now;
    public DateTime? AcknowledgedAt { get; set; }
    public string AcknowledgedBy { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
