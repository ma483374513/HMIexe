/// <summary>
/// 工程发布设置模型文件。
/// 定义发布目标平台枚举和可配置的发布参数，
/// 供发布服务在打包时使用。
/// </summary>
namespace HMIexe.Core.Models.Project;

/// <summary>
/// 发布目标平台枚举。
/// 标识最终可执行文件所运行的操作系统和 CPU 架构。
/// </summary>
public enum PublishPlatform
{
    /// <summary>Windows x64（生成 .exe 可执行文件）。</summary>
    WindowsX64,

    /// <summary>Linux x64（生成 ELF 可执行文件）。</summary>
    LinuxX64,

    /// <summary>Linux ARM64（生成适用于 ARM 设备的 ELF 可执行文件）。</summary>
    LinuxArm64
}

/// <summary>
/// 脚本编译/运行模式枚举。
/// </summary>
public enum ScriptMode
{
    /// <summary>调试模式：保留符号信息，输出详细日志，方便开发阶段定位问题。</summary>
    Debug,

    /// <summary>发布模式：启用编译优化，减小体积，适合部署到生产设备。</summary>
    Release
}

/// <summary>
/// 工程发布参数配置。
/// 包含目标平台、分辨率、资源优化和脚本模式等所有发布相关选项。
/// </summary>
public class PublishSettings
{
    /// <summary>目标发布平台，决定生成的可执行文件格式和运行环境。</summary>
    public PublishPlatform Platform { get; set; } = PublishPlatform.WindowsX64;

    /// <summary>运行时目标画面宽度（像素）。</summary>
    public int ResolutionWidth { get; set; } = 1920;

    /// <summary>运行时目标画面高度（像素）。</summary>
    public int ResolutionHeight { get; set; } = 1080;

    /// <summary>
    /// 是否启用资源加载优化。
    /// 启用后，发布服务将对图片等资源进行压缩和合并，以减小包体积并加快启动速度。
    /// </summary>
    public bool OptimizeResources { get; set; } = true;

    /// <summary>脚本编译模式：Debug 保留调试信息；Release 启用优化并去除符号。</summary>
    public ScriptMode ScriptMode { get; set; } = ScriptMode.Release;

    /// <summary>
    /// 是否将所有资源和项目数据内嵌到单一可执行文件中（Self-contained 单文件发布）。
    /// 禁用时，资源以独立文件夹形式与可执行文件并列输出。
    /// </summary>
    public bool SingleFilePublish { get; set; } = false;

    /// <summary>发布输出目录的绝对路径；为空时将在工程目录旁自动创建 publish/ 子目录。</summary>
    public string OutputDirectory { get; set; } = string.Empty;

    /// <summary>
    /// 将当前设置转换为对应的 .NET Runtime Identifier（RID）字符串，
    /// 供 <c>dotnet publish -r &lt;RID&gt;</c> 命令使用。
    /// </summary>
    public string ToRuntimeIdentifier() => Platform switch
    {
        PublishPlatform.WindowsX64 => "win-x64",
        PublishPlatform.LinuxX64   => "linux-x64",
        PublishPlatform.LinuxArm64 => "linux-arm64",
        _                          => "win-x64"
    };

    /// <summary>返回目标平台对应的可执行文件后缀（Windows 为 .exe，Linux 为空字符串）。</summary>
    public string ExecutableExtension() => Platform == PublishPlatform.WindowsX64 ? ".exe" : string.Empty;
}
