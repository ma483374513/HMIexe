/// <summary>
/// 应用程序主类文件。
/// 负责 Avalonia 应用的初始化、依赖注入容器的构建，以及主窗口的创建与启动。
/// </summary>
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using HMIexe.App.Services;
using HMIexe.App.ViewModels;
using HMIexe.Core.Services;
using HMIexe.Runtime.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HMIexe.App;

/// <summary>
/// HMI 组态软件的应用程序类。
/// 继承自 Avalonia 的 <see cref="Application"/>，负责加载 XAML 资源、
/// 构建依赖注入容器，并在框架初始化完成后创建主窗口。
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// 依赖注入服务提供器实例，在框架初始化完成后由 <see cref="BuildServices"/> 赋值。
    /// </summary>
    private IServiceProvider? _services;

    /// <summary>
    /// 初始化应用程序，加载 App.axaml 中定义的 XAML 资源（样式、主题等）。
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// 框架初始化完成后的回调方法。
    /// 构建 DI 服务容器，解析主窗口 ViewModel，并创建主窗口实例。
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        _services = BuildServices();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 解析设计器 ViewModel 并设置为主窗口的初始内容
            var designerVm = _services.GetRequiredService<DesignerViewModel>();
            var mainVm = _services.GetRequiredService<MainWindowViewModel>();
            mainVm.CurrentContent = designerVm;

            desktop.MainWindow = new MainWindow { DataContext = mainVm };
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// 构建并配置应用程序的依赖注入服务容器。
    /// 按照核心服务 → 运行时服务 → 应用服务 → ViewModel 的顺序注册所有单例。
    /// </summary>
    /// <returns>构建完成的 <see cref="IServiceProvider"/> 实例。</returns>
    private static IServiceProvider BuildServices()
    {
        var services = new ServiceCollection();

        services.AddLogging();

        // 核心业务服务：工程管理、变量、报警、脚本、通信
        services.AddSingleton<IProjectService, ProjectService>();
        services.AddSingleton<IVariableService, VariableService>();
        services.AddSingleton<IAlarmService, AlarmService>();
        services.AddSingleton<IScriptService, ScriptService>();
        services.AddSingleton<ICommunicationService, CommunicationService>();

        // 运行时服务：报警条件求值器、数据持久化服务
        services.AddSingleton<AlarmConditionEvaluator>();
        services.AddSingleton<DataPersistenceService>();

        // 应用层服务：UI 对话框抽象
        services.AddSingleton<IDialogService, DialogService>();

        // 各功能模块的 ViewModel
        services.AddSingleton<DesignerViewModel>();
        services.AddSingleton<VariableManagerViewModel>();
        services.AddSingleton<ScriptEditorViewModel>();
        services.AddSingleton<AlarmManagerViewModel>();
        services.AddSingleton<CommunicationManagerViewModel>();
        services.AddSingleton<ResourceManagerViewModel>();
        services.AddSingleton<RuntimeViewModel>();
        services.AddSingleton<MainWindowViewModel>();

        return services.BuildServiceProvider();
    }
}