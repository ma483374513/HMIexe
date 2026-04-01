namespace HMIexe.App.Services;

public interface IDialogService
{
    Task<string?> OpenFileAsync(string title, IEnumerable<FileFilter> filters);
    Task<string?> SaveFileAsync(string title, IEnumerable<FileFilter> filters, string? defaultFileName = null);
    Task ShowMessageAsync(string title, string message);
    Task<bool> ShowConfirmAsync(string title, string message);
    Task<string?> ShowInputAsync(string title, string prompt, string? defaultValue = null);
}

public record FileFilter(string Name, IEnumerable<string> Extensions);
