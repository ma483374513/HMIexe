using System.Reflection;

/// <summary>
/// 提供 HMI 插件的动态加载与卸载管理功能。
/// <see cref="PluginManager"/> 通过反射扫描程序集，发现并实例化所有实现了
/// <see cref="IHmiPlugin"/> 的类型，完成插件的初始化和生命周期管理。
/// </summary>
namespace HMIexe.Plugins;

/// <summary>
/// 插件管理器，负责在运行时动态加载插件程序集、维护已加载插件的注册表，
/// 并在插件卸载时调用其 <see cref="IHmiPlugin.Shutdown"/> 方法以释放资源。
/// </summary>
public class PluginManager
{
    /// <summary>
    /// 已加载插件的内部字典，键为插件的 <see cref="IHmiPlugin.Id"/>，值为插件实例。
    /// </summary>
    private readonly Dictionary<string, IHmiPlugin> _plugins = new();

    /// <summary>
    /// 插件宿主引用，在插件初始化时传入，供插件注册控件和访问运行时服务。
    /// </summary>
    private readonly IPluginHost _host;

    /// <summary>
    /// 初始化 <see cref="PluginManager"/> 的新实例。
    /// </summary>
    /// <param name="host">插件宿主，提供控件注册、日志及变量访问能力。</param>
    public PluginManager(IPluginHost host)
    {
        _host = host;
    }

    /// <summary>
    /// 获取当前已成功加载的所有插件的只读字典视图，键为插件 ID。
    /// </summary>
    public IReadOnlyDictionary<string, IHmiPlugin> LoadedPlugins => _plugins;

    /// <summary>
    /// 异步从指定程序集路径加载所有 HMI 插件。
    /// 方法通过反射扫描程序集中所有实现了 <see cref="IHmiPlugin"/> 的非抽象类型，
    /// 依次实例化、注册并调用 <see cref="IHmiPlugin.Initialize"/> 完成初始化。
    /// </summary>
    /// <param name="assemblyPath">插件程序集的完整文件路径（.dll 文件）。</param>
    /// <returns>至少成功加载一个插件（或程序集加载本身无异常）时返回 <c>true</c>；发生异常时返回 <c>false</c>。</returns>
    public async Task<bool> LoadPluginAsync(string assemblyPath)
    {
        try
        {
            // 使用 Assembly.LoadFrom 从指定路径加载程序集（基于文件路径，支持从任意目录加载）
            var assembly = Assembly.LoadFrom(assemblyPath);

            // 筛选出程序集中所有实现了 IHmiPlugin 且非抽象的类型（排除接口和抽象类自身）
            var pluginTypes = assembly.GetTypes()
                .Where(t => typeof(IHmiPlugin).IsAssignableFrom(t) && !t.IsAbstract);

            foreach (var type in pluginTypes)
            {
                // 使用无参构造函数反射实例化插件，并检查是否为 IHmiPlugin 的有效实例
                if (Activator.CreateInstance(type) is IHmiPlugin plugin)
                {
                    // 以插件 ID 为键存储实例（同 ID 的后续加载会覆盖已有实例）
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

    /// <summary>
    /// 卸载指定 ID 的插件，调用其 <see cref="IHmiPlugin.Shutdown"/> 方法并从注册表中移除。
    /// 若指定 ID 的插件不存在，则方法静默返回，不抛出异常。
    /// </summary>
    /// <param name="pluginId">要卸载的插件唯一标识符。</param>
    public void UnloadPlugin(string pluginId)
    {
        if (_plugins.TryGetValue(pluginId, out var plugin))
        {
            // 先调用插件的关闭方法，确保资源得到释放，再从字典中移除
            plugin.Shutdown();
            _plugins.Remove(pluginId);
        }
    }
}
