/// <summary>
/// 应用程序入口文件。
/// 负责配置并启动基于 Avalonia UI 框架的 HMI 组态软件。
/// </summary>
using Avalonia;
using System;

namespace HMIexe.App;

/// <summary>
/// 应用程序入口点类。
/// 包含 Avalonia 应用的启动配置与主函数。
/// </summary>
class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    /// <summary>
    /// 应用程序主入口方法。
    /// 使用经典桌面生命周期启动 Avalonia 应用。
    /// </summary>
    /// <param name="args">命令行参数。</param>
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    /// <summary>
    /// 构建并配置 Avalonia 应用实例。
    /// 启用平台自动检测、Inter 字体支持和追踪日志输出。
    /// 此方法同时被可视化设计器调用，请勿删除。
    /// </summary>
    /// <returns>配置完成的 <see cref="AppBuilder"/> 实例。</returns>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
