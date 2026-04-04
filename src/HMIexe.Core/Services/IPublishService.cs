/// <summary>
/// 工程发布服务接口文件。
/// 定义发布操作的契约，供 UI 层调用，实现层在 HMIexe.Runtime 中提供。
/// </summary>
using HMIexe.Core.Models.Project;

namespace HMIexe.Core.Services;

/// <summary>
/// HMI 工程发布服务接口。
/// 负责将已设计的 HMI 工程打包为目标平台可执行的发布包。
/// </summary>
public interface IPublishService
{
    /// <summary>
    /// 异步发布当前 HMI 工程。
    /// 根据 <paramref name="settings"/> 中的参数，将工程数据和运行时合并，
    /// 输出目标平台的可执行包到指定目录。
    /// </summary>
    /// <param name="project">要发布的 HMI 工程模型。</param>
    /// <param name="settings">发布配置参数，包括平台、分辨率、脚本模式等。</param>
    /// <param name="progress">可选的进度报告回调，参数为当前进度描述文本。</param>
    /// <param name="cancellationToken">取消令牌，用于支持用户中途取消发布操作。</param>
    /// <returns>发布输出目录的绝对路径。</returns>
    Task<string> PublishAsync(
        HmiProject project,
        PublishSettings settings,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 检测当前运行环境是否安装了 .NET SDK，以确认是否能够执行 dotnet publish。
    /// </summary>
    /// <returns>若找到可用的 dotnet CLI 工具则返回 <c>true</c>。</returns>
    Task<bool> IsDotnetSdkAvailableAsync();
}
