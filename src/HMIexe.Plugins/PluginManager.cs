using System.Reflection;

namespace HMIexe.Plugins;

public class PluginManager
{
    private readonly Dictionary<string, IHmiPlugin> _plugins = new();
    private readonly IPluginHost _host;

    public PluginManager(IPluginHost host)
    {
        _host = host;
    }

    public IReadOnlyDictionary<string, IHmiPlugin> LoadedPlugins => _plugins;

    public async Task<bool> LoadPluginAsync(string assemblyPath)
    {
        try
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            var pluginTypes = assembly.GetTypes()
                .Where(t => typeof(IHmiPlugin).IsAssignableFrom(t) && !t.IsAbstract);

            foreach (var type in pluginTypes)
            {
                if (Activator.CreateInstance(type) is IHmiPlugin plugin)
                {
                    _plugins[plugin.Id] = plugin;
                    plugin.Initialize(_host);
                    _host.Log($"Plugin '{plugin.Name}' v{plugin.Version} loaded from {assemblyPath}", LogLevel.Info);
                }
            }
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _host.Log($"Failed to load plugin from '{assemblyPath}': {ex.Message}", LogLevel.Error);
            return false;
        }
    }

    public void UnloadPlugin(string pluginId)
    {
        if (_plugins.TryGetValue(pluginId, out var plugin))
        {
            plugin.Shutdown();
            _plugins.Remove(pluginId);
        }
    }
}
