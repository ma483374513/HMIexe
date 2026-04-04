using HMIexe.Core.Models.Project;

/// <summary>
/// 项目服务接口定义。
/// 提供 HMI 项目的创建、打开、保存、导出/导入及最近项目记录等能力，
/// 是设计器工程管理功能的核心契约。
/// </summary>
namespace HMIexe.Core.Services;

/// <summary>
/// HMI 项目服务接口，负责管理当前打开的工程文件及其持久化操作。
/// 消费者可订阅 <see cref="ProjectChanged"/> 事件以响应工程切换。
/// </summary>
public interface IProjectService
{
    /// <summary>当前打开的 HMI 项目；未打开任何项目时为 null。</summary>
    HmiProject? CurrentProject { get; }

    /// <summary>当前项目是否有未保存的更改；为 true 时应提示用户保存。</summary>
    bool IsDirty { get; }

    /// <summary>
    /// 异步创建一个新的 HMI 项目并将其设为当前项目。
    /// </summary>
    /// <param name="name">新项目的名称。</param>
    /// <returns>新创建的 <see cref="HmiProject"/> 实例。</returns>
    Task<HmiProject> CreateNewProjectAsync(string name);

    /// <summary>
    /// 异步从磁盘文件打开一个已有的 HMI 项目。
    /// </summary>
    /// <param name="filePath">项目文件（.hmi 或打包文件）的路径。</param>
    /// <returns>加载完成的 <see cref="HmiProject"/> 实例。</returns>
    Task<HmiProject> OpenProjectAsync(string filePath);

    /// <summary>异步保存当前项目到原始文件路径。</summary>
    Task SaveProjectAsync();

    /// <summary>
    /// 异步将当前项目另存为指定路径的新文件。
    /// </summary>
    /// <param name="filePath">目标保存路径。</param>
    Task SaveProjectAsAsync(string filePath);

    /// <summary>
    /// 异步将当前项目导出为可分发的打包文件（包含所有资源）。
    /// </summary>
    /// <param name="outputPath">导出包的目标路径。</param>
    /// <returns>导出文件的完整路径。</returns>
    Task<string> ExportProjectAsync(string outputPath);

    /// <summary>
    /// 异步从打包文件中导入一个 HMI 项目。
    /// </summary>
    /// <param name="packagePath">要导入的打包文件路径。</param>
    /// <returns>导入完成的 <see cref="HmiProject"/> 实例。</returns>
    Task<HmiProject> ImportProjectAsync(string packagePath);

    /// <summary>最近打开的项目文件路径列表，用于在欢迎界面快速访问。</summary>
    IReadOnlyList<string> RecentProjects { get; }

    /// <summary>当前项目发生切换（打开、新建或关闭）时引发的事件，参数为新的当前项目（可为 null）。</summary>
    event EventHandler<HmiProject?> ProjectChanged;
}
