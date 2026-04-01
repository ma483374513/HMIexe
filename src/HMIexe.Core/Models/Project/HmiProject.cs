namespace HMIexe.Core.Models.Project;

public class HmiProject
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "New Project";
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime ModifiedAt { get; set; } = DateTime.Now;
    public string DefaultPageId { get; set; } = string.Empty;
    public List<Canvas.HmiPage> Pages { get; set; } = new();
    public List<Variables.HmiVariable> Variables { get; set; } = new();
    public List<Script.HmiScript> Scripts { get; set; } = new();
    public List<Resource.HmiResource> Resources { get; set; } = new();
    public List<Communication.CommunicationChannel> CommunicationChannels { get; set; } = new();
    public List<Alarm.HmiAlarmDefinition> AlarmDefinitions { get; set; } = new();
    public ProjectSettings Settings { get; set; } = new();
}

public class ProjectSettings
{
    public string DefaultLanguage { get; set; } = "zh-CN";
    public List<string> SupportedLanguages { get; set; } = new() { "zh-CN", "en-US" };
    public string Theme { get; set; } = "Dark";
    public double TargetWidth { get; set; } = 1920;
    public double TargetHeight { get; set; } = 1080;
    public bool AutoSave { get; set; } = true;
    public int AutoSaveIntervalSeconds { get; set; } = 60;
}
