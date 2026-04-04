/// <summary>
/// HMI 运行画面播放器入口文件。
/// 读取命令行参数或同目录 project.hmiproj，启动 Avalonia 运行时播放器。
/// </summary>
using Avalonia;

namespace HMIexe.RuntimePlayer;

/// <summary>
/// 运行画面播放器程序入口。
/// </summary>
class Program
{
    /// <summary>
    /// 应用程序入口方法。
    /// 支持通过命令行参数指定工程文件路径：
    /// <code>HMIexe.Player [project-file]</code>
    /// 若未提供参数，则在可执行文件同目录下查找 project.hmiproj。
    /// </summary>
    [STAThread]
    public static void Main(string[] args)
    {
        // 确定工程文件路径：优先使用命令行参数，其次查找同目录文件
        var projectFile = args.Length > 0 && File.Exists(args[0])
            ? Path.GetFullPath(args[0])
            : FindDefaultProjectFile();

        BuildAvaloniaApp(projectFile).StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// 构建并配置 Avalonia 应用，将工程文件路径传入 App。
    /// </summary>
    public static AppBuilder BuildAvaloniaApp(string? projectFile = null)
        => AppBuilder.Configure(() => new App(projectFile))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    /// <summary>
    /// 在可执行文件所在目录搜索默认工程文件 project.hmiproj。
    /// </summary>
    private static string? FindDefaultProjectFile()
    {
        var dir = AppContext.BaseDirectory;
        var candidate = Path.Combine(dir, "project.hmiproj");
        return File.Exists(candidate) ? candidate : null;
    }
}
