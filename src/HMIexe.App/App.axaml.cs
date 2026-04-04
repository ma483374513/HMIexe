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

public partial class App : Application
{
    private IServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _services = BuildServices();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var designerVm = _services.GetRequiredService<DesignerViewModel>();
            var mainVm = _services.GetRequiredService<MainWindowViewModel>();
            mainVm.CurrentContent = designerVm;

            desktop.MainWindow = new MainWindow { DataContext = mainVm };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static IServiceProvider BuildServices()
    {
        var services = new ServiceCollection();

        services.AddLogging();

        // Core services
        services.AddSingleton<IProjectService, ProjectService>();
        services.AddSingleton<IVariableService, VariableService>();
        services.AddSingleton<IAlarmService, AlarmService>();
        services.AddSingleton<IScriptService, ScriptService>();
        services.AddSingleton<ICommunicationService, CommunicationService>();

        // Runtime services
        services.AddSingleton<AlarmConditionEvaluator>();

        // App services
        services.AddSingleton<IDialogService, DialogService>();

        // ViewModels
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