namespace HMIexe.Core.Models.Script;

public enum ScriptTriggerType
{
    Manual,
    Timer,
    Loop,
    VariableChange,
    PageLoad,
    PageClose,
    ControlEvent
}

public class HmiScript
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Script";
    public string Code { get; set; } = string.Empty;
    public ScriptTriggerType TriggerType { get; set; } = ScriptTriggerType.Manual;
    public int TimerIntervalMs { get; set; } = 1000;
    public string TriggerVariableId { get; set; } = string.Empty;
    public string TriggerControlId { get; set; } = string.Empty;
    public string TriggerEvent { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public string Description { get; set; } = string.Empty;
}
