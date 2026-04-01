namespace HMIexe.Plugins;

public interface IHmiPlugin
{
    string Id { get; }
    string Name { get; }
    string Version { get; }
    string Description { get; }
    string Author { get; }
    string IconPath { get; }
    IReadOnlyList<IPluginControlDescriptor> Controls { get; }
    void Initialize(IPluginHost host);
    void Shutdown();
}

public interface IPluginControlDescriptor
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    string IconPath { get; }
    string ThumbnailPath { get; }
    string Category { get; }
    Type ControlModelType { get; }
    IReadOnlyList<PluginPropertyDescriptor> Properties { get; }
}

public class PluginPropertyDescriptor
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Type PropertyType { get; set; } = typeof(string);
    public object? DefaultValue { get; set; }
    public string Category { get; set; } = "General";
    public bool IsRequired { get; set; }
}

public interface IPluginHost
{
    void RegisterControl(IPluginControlDescriptor descriptor);
    void UnregisterControl(string controlId);
    void Log(string message, LogLevel level = LogLevel.Info);
    object? GetVariable(string name);
    void SetVariable(string name, object? value);
}

public enum LogLevel { Debug, Info, Warning, Error, Critical }
