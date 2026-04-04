/// <summary>
/// HMI 运行画面播放器应用类。
/// 负责 DI 容器构建，加载工程文件，并启动播放器主窗口。
/// </summary>
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using HMIexe.Core.Services;
using HMIexe.Runtime.Services;
using HMIexe.RuntimePlayer.ViewModels;
using HMIexe.RuntimePlayer.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HMIexe.RuntimePlayer;

/// <summary>
/// 运行画面播放器应用。
/// 与设计器（HMIexe.App）共享核心服务和运行时服务，
/// 但不包含任何设计器 UI 组件，仅负责加载并运行 HMI 工程画面。
/// </summary>
public partial class App : Application
{
    /// <summary>要加载的工程文件绝对路径；为 null 时显示"未找到工程"提示。</summary>
    private readonly string? _projectFilePath;

    /// <summary>依赖注入服务提供器。</summary>
    private IServiceProvider? _services;

    /// <summary>
    /// 无参构造函数，供 Avalonia XAML 加载器使用。
    /// </summary>
    public App() { }

    /// <summary>
    /// 初始化播放器应用，传入工程文件路径。
    /// </summary>
    /// <param name="projectFilePath">工程文件绝对路径，为 null 时工程文件不存在。</param>
    public App(string? projectFilePath)
    {
        _projectFilePath = projectFilePath;
    }

    /// <inheritdoc/>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <inheritdoc/>
    public override void OnFrameworkInitializationCompleted()
    {
        _services = BuildServices();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var playerVm = _services.GetRequiredService<PlayerViewModel>();
            var window = new PlayerWindow { DataContext = playerVm };

            // 异步加载工程（窗口显示后在 UI 线程执行）
            desktop.MainWindow = window;
            window.Opened += async (_, _) =>
            {
                await playerVm.LoadProjectAsync(_projectFilePath);
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// 构建运行时播放器所需的最小 DI 容器。
    /// 不包含设计器服务（报警编辑器、脚本编辑器等）。
    /// </summary>
    private static IServiceProvider BuildServices()
    {
        var services = new ServiceCollection();

        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));

        // 核心运行时服务
        services.AddSingleton<IProjectService, ProjectService>();
        services.AddSingleton<IVariableService, VariableService>();
        services.AddSingleton<ICommunicationService, CommunicationService>();

        // 播放器 ViewModel
        services.AddSingleton<PlayerViewModel>();

        return services.BuildServiceProvider();
    }
}
