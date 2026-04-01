using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace HMIexe.App.Services;

public class DialogService : IDialogService
{
    private static Window? GetMainWindow() =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
        ?.MainWindow;

    private static IStorageProvider? GetStorageProvider() =>
        GetMainWindow()?.StorageProvider;

    public async Task<string?> OpenFileAsync(string title, IEnumerable<FileFilter> filters)
    {
        var provider = GetStorageProvider();
        if (provider == null) return null;

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
            DefaultExtension = fileTypes.FirstOrDefault()?.Patterns?.FirstOrDefault()?.TrimStart('*', '.'),
            SuggestedFileName = defaultFileName,
            FileTypeChoices = fileTypes
        });

        return file?.TryGetLocalPath();
    }

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

        // Wire the OK button to close
        var panel = (StackPanel)dialog.Content!;
        ((Button)panel.Children[1]).Click += (_, _) => dialog.Close();

        await dialog.ShowDialog(window);
    }

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

        okButton.Click += (_, _) => { result = true; dialog.Close(); };
        cancelButton.Click += (_, _) => { result = false; dialog.Close(); };

        await dialog.ShowDialog(window);
        return result;
    }

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

        okButton.Click += (_, _) => { result = input.Text; dialog.Close(); };
        cancelButton.Click += (_, _) => { result = null; dialog.Close(); };

        await dialog.ShowDialog(window);
        return result;
    }
}
