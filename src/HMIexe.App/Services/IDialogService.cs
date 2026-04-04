/// <summary>
/// 对话框服务接口文件。
/// 提供文件选择、消息提示、确认询问和文本输入等通用 UI 弹窗交互功能的抽象定义。
/// </summary>
namespace HMIexe.App.Services;

/// <summary>
/// 对话框服务接口，定义应用程序中所有模态 UI 弹窗交互的抽象方法。
/// 通过依赖注入使用，使 ViewModel 能够触发 UI 对话框而无需直接依赖 Avalonia 控件。
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// 显示文件打开对话框，允许用户选择单个文件。
    /// </summary>
    /// <param name="title">对话框标题。</param>
    /// <param name="filters">文件类型过滤器列表。</param>
    /// <returns>用户选中的文件本地路径；若取消则返回 <c>null</c>。</returns>
    Task<string?> OpenFileAsync(string title, IEnumerable<FileFilter> filters);

    /// <summary>
    /// 显示文件保存对话框，允许用户指定保存路径和文件名。
    /// </summary>
    /// <param name="title">对话框标题。</param>
    /// <param name="filters">文件类型过滤器列表。</param>
    /// <param name="defaultFileName">默认建议文件名（可选）。</param>
    /// <returns>用户选定的保存路径；若取消则返回 <c>null</c>。</returns>
    Task<string?> SaveFileAsync(string title, IEnumerable<FileFilter> filters, string? defaultFileName = null);

    /// <summary>
    /// 显示信息提示对话框（仅含"确定"按钮）。
    /// </summary>
    /// <param name="title">对话框标题。</param>
    /// <param name="message">提示消息内容。</param>
    Task ShowMessageAsync(string title, string message);

    /// <summary>
    /// 显示确认对话框（含"确定"和"取消"按钮）。
    /// </summary>
    /// <param name="title">对话框标题。</param>
    /// <param name="message">确认消息内容。</param>
    /// <returns>用户点击"确定"时返回 <c>true</c>；点击"取消"时返回 <c>false</c>。</returns>
    Task<bool> ShowConfirmAsync(string title, string message);

    /// <summary>
    /// 显示文件夹选择对话框，允许用户选择单个目录。
    /// </summary>
    /// <param name="title">对话框标题。</param>
    /// <returns>用户选中的目录本地路径；若取消则返回 <c>null</c>。</returns>
    Task<string?> OpenFolderAsync(string title);

    /// <summary>
    /// 显示文本输入对话框，允许用户输入一行文本。
    /// </summary>
    /// <param name="title">对话框标题。</param>
    /// <param name="prompt">输入框上方的提示文字。</param>
    /// <param name="defaultValue">输入框的默认预填内容（可选）。</param>
    /// <returns>用户输入的文本；若取消则返回 <c>null</c>。</returns>
    Task<string?> ShowInputAsync(string title, string prompt, string? defaultValue = null);
}

/// <summary>
/// 文件过滤器记录类型，用于文件对话框中指定可选文件类型。
/// </summary>
/// <param name="Name">过滤器的显示名称（例如 "CSV文件"）。</param>
/// <param name="Extensions">对应的文件扩展名列表，不含前导点号（例如 "csv"）。</param>
public record FileFilter(string Name, IEnumerable<string> Extensions);
