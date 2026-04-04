/// <summary>
/// 对话框服务实现文件。
/// 基于 Avalonia StorageProvider 和内联构建的简单窗口，
/// 提供文件选择、消息提示、确认询问和文本输入等对话框功能。
/// </summary>
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace HMIexe.App.Services;

/// <summary>
/// <see cref="IDialogService"/> 的 Avalonia 实现。
/// 所有对话框均以主窗口为父窗口显示，确保模态行为正确。
/// </summary>
public class DialogService : IDialogService
{
    /// <summary>
    /// 获取应用程序的主窗口实例。
    /// </summary>
    /// <returns>主窗口实例；若应用程序生命周期不是桌面模式则返回 <c>null</c>。</returns>
    private static Window? GetMainWindow() =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
        ?.MainWindow;

    /// <summary>
    /// 获取主窗口的存储提供器，用于访问系统文件对话框。
    /// </summary>
    /// <returns>存储提供器实例；若主窗口不可用则返回 <c>null</c>。</returns>
    private static IStorageProvider? GetStorageProvider() =>
        GetMainWindow()?.StorageProvider;

    /// <summary>
    /// 显示文件打开对话框，允许用户选择单个文件。
    /// </summary>
    /// <param name="title">对话框标题。</param>
    /// <param name="filters">文件类型过滤器列表，用于限制可见文件类型。</param>
    /// <returns>用户选中文件的本地路径；若取消则返回 <c>null</c>。</returns>
    public async Task<string?> OpenFileAsync(string title, IEnumerable<FileFilter> filters)
    {
        var provider = GetStorageProvider();
        if (provider == null) return null;

        // 将自定义 FileFilter 转换为 Avalonia 的 FilePickerFileType，并添加通配符模式
        var fileTypes = filters.Select(f => new FilePickerFileType(f.Name)
        {
            Patterns = f.Extensions.Select(e => $"*.{e}").ToList()
        }).ToList();

        var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = fileTypes
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    /// <summary>
    /// 显示文件保存对话框，允许用户指定保存路径和文件名。
    /// </summary>
    /// <param name="title">对话框标题。</param>
    /// <param name="filters">文件类型过滤器列表。</param>
    /// <param name="defaultFileName">建议的默认文件名（可选）。</param>
    /// <returns>用户选定的保存路径；若取消则返回 <c>null</c>。</returns>
    public async Task<string?> SaveFileAsync(string title, IEnumerable<FileFilter> filters,
        string? defaultFileName = null)
    {
        var provider = GetStorageProvider();
        if (provider == null) return null;

        var fileTypes = filters.Select(f => new FilePickerFileType(f.Name)
        {
            Patterns = f.Extensions.Select(e => $"*.{e}").ToList()
        }).ToList();

        var file = await provider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            // 从第一个过滤器的第一个模式中提取默认扩展名（去掉 "*." 前缀）
            DefaultExtension = fileTypes.FirstOrDefault()?.Patterns?.FirstOrDefault()?.TrimStart('*', '.'),
            SuggestedFileName = defaultFileName,
            FileTypeChoices = fileTypes
        });

        return file?.TryGetLocalPath();
    }

    /// <summary>
    /// 显示文件夹选择对话框，允许用户选择单个目录。
    /// </summary>
    /// <param name="title">对话框标题。</param>
    /// <returns>用户选中目录的本地路径；若取消则返回 <c>null</c>。</returns>
    public async Task<string?> OpenFolderAsync(string title)
    {
        var provider = GetStorageProvider();
        if (provider == null) return null;

        var folders = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });

        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }

    /// <summary>
    /// 显示仅含"确定"按钮的信息提示对话框。
    /// 对话框以主窗口为父窗口居中显示。
    /// </summary>
    /// <param name="title">对话框标题。</param>
    /// <param name="message">显示的消息内容，支持自动换行。</param>
    public async Task ShowMessageAsync(string title, string message)
    {
        var window = GetMainWindow();
        if (window == null) return;

        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 16,
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    new Button
                    {
                        Content = "确定",
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        MinWidth = 80
                    }
                }
            }
        };

        // 将"确定"按钮的点击事件绑定到关闭对话框
        var panel = (StackPanel)dialog.Content!;
        ((Button)panel.Children[1]).Click += (_, _) => dialog.Close();

        await dialog.ShowDialog(window);
    }

    /// <summary>
    /// 显示含"确定"和"取消"按钮的确认对话框。
    /// </summary>
    /// <param name="title">对话框标题。</param>
    /// <param name="message">显示的确认消息内容。</param>
    /// <returns>用户点击"确定"时返回 <c>true</c>；点击"取消"时返回 <c>false</c>。</returns>
    public async Task<bool> ShowConfirmAsync(string title, string message)
    {
        var window = GetMainWindow();
        if (window == null) return false;

        var result = false;
        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var okButton = new Button { Content = "确定", MinWidth = 80 };
        var cancelButton = new Button { Content = "取消", MinWidth = 80, Margin = new Thickness(8, 0, 0, 0) };

        var buttons = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Children = { okButton, cancelButton }
        };

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 16,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                buttons
            }
        };

        // 确定按钮：将 result 设为 true 后关闭；取消按钮：保持 false 后关闭
        okButton.Click += (_, _) => { result = true; dialog.Close(); };
        cancelButton.Click += (_, _) => { result = false; dialog.Close(); };

        await dialog.ShowDialog(window);
        return result;
    }

    /// <summary>
    /// 显示含单行文本输入框的输入对话框。
    /// </summary>
    /// <param name="title">对话框标题。</param>
    /// <param name="prompt">输入框上方的提示文字。</param>
    /// <param name="defaultValue">输入框的默认预填内容（可选）。</param>
    /// <returns>用户确认输入的文本；若取消则返回 <c>null</c>。</returns>
    public async Task<string?> ShowInputAsync(string title, string prompt, string? defaultValue = null)
    {
        var window = GetMainWindow();
        if (window == null) return null;

        string? result = null;
        var input = new TextBox { Text = defaultValue, Margin = new Thickness(0, 4, 0, 0) };
        var okButton = new Button { Content = "确定", MinWidth = 80 };
        var cancelButton = new Button { Content = "取消", MinWidth = 80, Margin = new Thickness(8, 0, 0, 0) };

        var dialog = new Window
        {
            Title = title,
            Width = 420,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 12,
                Children =
                {
                    new TextBlock { Text = prompt },
                    input,
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Children = { okButton, cancelButton }
                    }
                }
            }
        };

        // 确定时读取输入框文本；取消时结果保持为 null
        okButton.Click += (_, _) => { result = input.Text; dialog.Close(); };
        cancelButton.Click += (_, _) => { result = null; dialog.Close(); };

        await dialog.ShowDialog(window);
        return result;
    }
}
