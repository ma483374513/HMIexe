/// <summary>
/// 定义 HMI 插件体系的核心接口、描述符类及枚举类型。
/// 插件通过实现 <see cref="IHmiPlugin"/> 向宿主注册自定义控件，
/// 并通过 <see cref="IPluginHost"/> 与宿主运行时交互（控件注册、日志记录、变量读写）。
/// </summary>
namespace HMIexe.Plugins;

/// <summary>
/// HMI 插件接口，定义插件的元数据属性、控件列表及生命周期方法。
/// 每个插件程序集可包含多个实现此接口的类；<see cref="PluginManager"/> 负责发现并加载它们。
/// </summary>
public interface IHmiPlugin
{
    /// <summary>
    /// 插件的唯一标识符，在整个应用程序中应保持全局唯一（建议使用反向域名格式）。
    /// </summary>
    string Id { get; }

    /// <summary>
    /// 插件的显示名称，用于 UI 展示。
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 插件的版本号字符串（例如 "1.0.0"）。
    /// </summary>
    string Version { get; }

    /// <summary>
    /// 对插件功能的简要描述，供用户了解插件用途。
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 插件作者或组织名称。
    /// </summary>
    string Author { get; }

    /// <summary>
    /// 插件图标的资源路径，用于在插件列表中展示图标。
    /// </summary>
    string IconPath { get; }

    /// <summary>
    /// 该插件提供的所有控件描述符集合；每个描述符对应一种可拖拽到画布的 HMI 控件。
    /// </summary>
    IReadOnlyList<IPluginControlDescriptor> Controls { get; }

    /// <summary>
    /// 初始化插件，在插件被 <see cref="PluginManager"/> 加载后立即调用。
    /// 插件应在此方法中向 <paramref name="host"/> 注册控件并完成资源初始化。
    /// </summary>
    /// <param name="host">插件宿主接口，提供控件注册、日志及变量访问能力。</param>
    void Initialize(IPluginHost host);

    /// <summary>
    /// 关闭插件，在插件被卸载前调用。
    /// 插件应在此方法中释放资源、注销控件并执行必要的清理工作。
    /// </summary>
    void Shutdown();
}

/// <summary>
/// HMI 控件描述符接口，描述插件提供的单个可视化控件的元数据及属性定义。
/// 宿主运行时根据此描述符在控件工具箱中展示控件，并在实例化时创建对应的控件模型。
/// </summary>
public interface IPluginControlDescriptor
{
    /// <summary>
    /// 控件的唯一标识符，在同一插件内应保持唯一。
    /// </summary>
    string Id { get; }

    /// <summary>
    /// 控件的显示名称，用于工具箱和属性面板中展示。
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 对控件功能的简要描述，供设计器用户参考。
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 控件图标的资源路径，用于在工具箱中展示控件图标。
    /// </summary>
    string IconPath { get; }

    /// <summary>
    /// 控件缩略图的资源路径，用于在选择器或预览面板中展示控件外观。
    /// </summary>
    string ThumbnailPath { get; }

    /// <summary>
    /// 控件所属的分类名称，用于在工具箱中将控件分组展示（例如 "仪表"、"按钮"）。
    /// </summary>
    string Category { get; }

    /// <summary>
    /// 与该控件关联的模型类型，宿主将通过此类型实例化控件的数据模型。
    /// </summary>
    Type ControlModelType { get; }

    /// <summary>
    /// 控件支持的可配置属性描述符列表，供属性面板动态生成编辑界面。
    /// </summary>
    IReadOnlyList<PluginPropertyDescriptor> Properties { get; }
}

/// <summary>
/// 描述控件单个可配置属性的元数据，包括名称、类型、默认值及分类信息。
/// 宿主运行时根据此描述符在属性面板中自动生成对应的编辑控件。
/// </summary>
public class PluginPropertyDescriptor
{
    /// <summary>
    /// 属性的编程名称，对应控件模型类中的实际属性名。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 属性在 UI 属性面板中的显示名称（可包含空格和本地化文本）。
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 对该属性用途的简要说明，通常显示为工具提示。
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 属性的 .NET 类型，用于属性面板选择合适的编辑器控件。
    /// </summary>
    public Type PropertyType { get; set; } = typeof(string);

    /// <summary>
    /// 属性的默认值；新建控件实例时将使用此值初始化属性。
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// 属性在属性面板中所属的分组名称，默认为 "General"（常规）。
    /// </summary>
    public string Category { get; set; } = "General";

    /// <summary>
    /// 指示该属性是否为必填项；<c>true</c> 时宿主应在验证阶段强制要求用户填写。
    /// </summary>
    public bool IsRequired { get; set; }
}

/// <summary>
/// 插件宿主接口，为插件提供与 HMI 运行时环境交互的能力，
/// 包括控件注册/注销、日志记录以及运行时变量的读写访问。
/// </summary>
public interface IPluginHost
{
    /// <summary>
    /// 向宿主注册一个控件描述符，使该控件出现在设计器的工具箱中。
    /// </summary>
    /// <param name="descriptor">要注册的控件描述符。</param>
    void RegisterControl(IPluginControlDescriptor descriptor);

    /// <summary>
    /// 从宿主注销指定 ID 的控件，控件将从工具箱中移除。
    /// </summary>
    /// <param name="controlId">要注销的控件唯一标识符。</param>
    void UnregisterControl(string controlId);

    /// <summary>
    /// 向宿主日志系统输出一条日志消息。
    /// </summary>
    /// <param name="message">日志消息内容。</param>
    /// <param name="level">日志级别，默认为 <see cref="LogLevel.Info"/>。</param>
    void Log(string message, LogLevel level = LogLevel.Info);

    /// <summary>
    /// 从宿主运行时变量表中读取指定名称的变量值。
    /// </summary>
    /// <param name="name">变量名称。</param>
    /// <returns>变量的当前值；若变量不存在则返回 <c>null</c>。</returns>
    object? GetVariable(string name);

    /// <summary>
    /// 向宿主运行时变量表中写入或更新指定名称的变量值。
    /// </summary>
    /// <param name="name">变量名称。</param>
    /// <param name="value">要设置的值；传入 <c>null</c> 可清除变量值。</param>
    void SetVariable(string name, object? value);
}

/// <summary>
/// 日志级别枚举，定义从调试到严重错误的五个标准日志等级。
/// 与常见日志框架（如 Microsoft.Extensions.Logging）的级别语义保持一致。
/// </summary>
public enum LogLevel
{
    /// <summary>调试级别：最详细的诊断信息，通常仅在开发阶段启用。</summary>
    Debug,
    /// <summary>信息级别：正常运行流程的记录信息。</summary>
    Info,
    /// <summary>警告级别：潜在问题或非预期状态，但程序仍可正常运行。</summary>
    Warning,
    /// <summary>错误级别：发生了错误，某些功能无法完成，但程序可以继续运行。</summary>
    Error,
    /// <summary>严重级别：发生了致命错误，程序可能无法继续正常运行。</summary>
    Critical
}
