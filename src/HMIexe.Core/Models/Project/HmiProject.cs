/// <summary>
/// HMI 项目模型定义。
/// 包含项目的元数据、所有页面、变量、脚本、资源、通信通道、报警定义及全局设置。
/// HmiProject 是整个 HMI 工程的根数据模型，通过序列化持久化到磁盘。
/// </summary>
namespace HMIexe.Core.Models.Project;

/// <summary>
/// HMI 项目，表示一个完整的 HMI 工程文件的根数据模型。
/// 包含工程中所有设计元素的集合，是保存、加载和导出操作的顶层对象。
/// </summary>
public class HmiProject
{
    /// <summary>项目的唯一标识符（GUID）。</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>项目名称，默认为 "New Project"。</summary>
    public string Name { get; set; } = "New Project";

    /// <summary>项目描述信息，便于版本管理和文档记录。</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>项目版本号，遵循语义化版本规范（SemVer），默认为 "1.0.0"。</summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>项目创建时间，默认为创建对象时的当前时间。</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>项目最后修改时间，每次保存时应更新此字段。</summary>
    public DateTime ModifiedAt { get; set; } = DateTime.Now;

    /// <summary>项目启动时默认显示的页面 ID；对应 Pages 列表中某个页面的 Id。</summary>
    public string DefaultPageId { get; set; } = string.Empty;

    /// <summary>项目包含的所有 HMI 页面列表。</summary>
    public List<Canvas.HmiPage> Pages { get; set; } = new();

    /// <summary>项目定义的所有 HMI 变量列表，用于数据绑定和脚本访问。</summary>
    public List<Variables.HmiVariable> Variables { get; set; } = new();

    /// <summary>项目包含的所有脚本定义列表。</summary>
    public List<Script.HmiScript> Scripts { get; set; } = new();

    /// <summary>项目引用的所有资源文件列表（图片、字体、音视频等）。</summary>
    public List<Resource.HmiResource> Resources { get; set; } = new();

    /// <summary>项目配置的所有通信通道列表，用于连接外部设备或数据源。</summary>
    public List<Communication.CommunicationChannel> CommunicationChannels { get; set; } = new();

    /// <summary>项目定义的所有报警规则列表。</summary>
    public List<Alarm.HmiAlarmDefinition> AlarmDefinitions { get; set; } = new();

    /// <summary>项目全局设置，包括语言、主题、目标分辨率和自动保存配置。</summary>
    public ProjectSettings Settings { get; set; } = new();
}

/// <summary>
/// HMI 项目全局设置。
/// 存储影响整个项目行为的全局配置项，如语言、UI 主题、目标分辨率和自动保存策略。
/// </summary>
public class ProjectSettings
{
    /// <summary>项目默认语言区域代码（IETF BCP 47 格式），默认为简体中文 "zh-CN"。</summary>
    public string DefaultLanguage { get; set; } = "zh-CN";

    /// <summary>项目支持的所有语言区域代码列表，用于多语言切换功能。</summary>
    public List<string> SupportedLanguages { get; set; } = new() { "zh-CN", "en-US" };

    /// <summary>项目 UI 主题标识，默认为 "Dark"（深色主题）。</summary>
    public string Theme { get; set; } = "Dark";

    /// <summary>目标显示设备的水平分辨率（像素），默认为 1920（全高清）。</summary>
    public double TargetWidth { get; set; } = 1920;

    /// <summary>目标显示设备的垂直分辨率（像素），默认为 1080（全高清）。</summary>
    public double TargetHeight { get; set; } = 1080;

    /// <summary>是否启用自动保存功能，默认为 true。</summary>
    public bool AutoSave { get; set; } = true;

    /// <summary>自动保存的间隔时间（秒），默认为 60 秒。</summary>
    public int AutoSaveIntervalSeconds { get; set; } = 60;
}
