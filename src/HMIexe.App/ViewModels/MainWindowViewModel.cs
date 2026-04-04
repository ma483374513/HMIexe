/// <summary>
/// 主窗口视图模型文件。
/// 负责工程的新建/打开/保存操作、各功能面板的切换、撤销/重做、
/// 主题切换以及运行时预览的协调管理。
/// </summary>
using System.Collections.ObjectModel;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HMIexe.App.Services;
using HMIexe.Core.Services;

namespace HMIexe.App.ViewModels;

/// <summary>
/// 主窗口视图模型。
/// 作为应用程序的顶层 ViewModel，持有所有子功能面板的 ViewModel 引用，
/// 并提供工程文件管理、面板导航、编辑操作和全局主题控制等命令。
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    /// <summary>工程业务服务，提供工程文件的创建、打开、保存功能。</summary>
    private readonly IProjectService _projectService;

    /// <summary>对话框服务，用于文件选择、确认和输入弹窗。</summary>
    private readonly IDialogService _dialogService;

    /// <summary>主窗口标题文本，包含工程名称。</summary>
    [ObservableProperty]
    private string _title = "HMI Designer";

    /// <summary>主内容区域的当前显示内容，通过 DataTemplate 匹配对应视图。</summary>
    [ObservableProperty]
    private object? _currentContent;

    /// <summary>指示当前是否有工程处于打开状态，控制依赖工程的 UI 元素的可用性。</summary>
    [ObservableProperty]
    private bool _isProjectOpen;

    /// <summary>底部状态栏的文本，显示最近操作的简短描述。</summary>
    [ObservableProperty]
    private string _statusText = "就绪";

    /// <summary>设计器视图模型，提供画布编辑功能。</summary>
    [ObservableProperty]
    private DesignerViewModel? _designerViewModel;

    /// <summary>指示当前是否使用深色主题。</summary>
    [ObservableProperty]
    private bool _isDarkTheme = true;

    /// <summary>最近打开的工程文件路径列表，用于快速重新打开。</summary>
    public ObservableCollection<string> RecentProjects { get; } = new();

    /// <summary>变量管理器视图模型，提供变量的增删改和 CSV 导入导出。</summary>
    public VariableManagerViewModel VariableManager { get; }

    /// <summary>脚本编辑器视图模型，提供 C# 脚本的编写、编译和运行。</summary>
    public ScriptEditorViewModel ScriptEditor { get; }

    /// <summary>报警管理器视图模型，提供报警定义和报警确认功能。</summary>
    public AlarmManagerViewModel AlarmManager { get; }

    /// <summary>通信管理器视图模型，提供通信通道配置和读写测试功能。</summary>
    public CommunicationManagerViewModel CommunicationManager { get; }

    /// <summary>资源管理器视图模型，提供图片、音频等资源的导入和管理。</summary>
    public ResourceManagerViewModel ResourceManager { get; }

    /// <summary>运行时预览视图模型，在设计模式下模拟控件与变量的绑定效果。</summary>
    public RuntimeViewModel RuntimePreview { get; }

    /// <summary>
    /// 初始化主窗口视图模型。
    /// 注入所有子 ViewModel 和服务，订阅工程变更事件，并加载最近工程列表。
    /// </summary>
    public MainWindowViewModel(
        IProjectService projectService,
        IDialogService dialogService,
        DesignerViewModel designerViewModel,
        VariableManagerViewModel variableManager,
        ScriptEditorViewModel scriptEditor,
        AlarmManagerViewModel alarmManager,
        CommunicationManagerViewModel communicationManager,
        ResourceManagerViewModel resourceManager,
        RuntimeViewModel runtimePreview)
    {
        _projectService = projectService;
        _dialogService = dialogService;
        DesignerViewModel = designerViewModel;
        VariableManager = variableManager;
        ScriptEditor = scriptEditor;
        AlarmManager = alarmManager;
        CommunicationManager = communicationManager;
        ResourceManager = resourceManager;
        RuntimePreview = runtimePreview;
        CurrentContent = designerViewModel;

        _projectService.ProjectChanged += OnProjectChanged;

        foreach (var recent in _projectService.RecentProjects)
            RecentProjects.Add(recent);
    }

    /// <summary>
    /// 工程变更事件处理器。
    /// 当工程加载或关闭时，更新标题栏、各子 ViewModel 的数据以及工程打开状态。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="project">新加载的工程对象；若工程已关闭则为 <c>null</c>。</param>
    private void OnProjectChanged(object? sender, Core.Models.Project.HmiProject? project)
    {
        if (project != null)
        {
            Title = $"HMI Designer - {project.Name}";
            IsProjectOpen = true;
            // 将工程数据分发给各子 ViewModel 加载
            DesignerViewModel?.LoadProject(project);
            ScriptEditor.LoadFromProject(project.Scripts);
            AlarmManager.LoadFromProject(project.AlarmDefinitions);
            CommunicationManager.LoadFromProject(project.CommunicationChannels);
            ResourceManager.LoadFromProject(project.Resources);
        }
        else
        {
            Title = "HMI Designer";
            IsProjectOpen = false;
        }
    }

    /// <summary>
    /// 新建工程命令。若当前工程有未保存修改，先询问是否保存，再提示输入新工程名称。
    /// </summary>
    [RelayCommand]
    private async Task NewProject()
    {
        if (_projectService.IsDirty)
        {
            var save = await _dialogService.ShowConfirmAsync("保存", "当前工程已修改，是否保存？");
            if (save) await SaveProject();
        }

        var name = await _dialogService.ShowInputAsync("新建工程", "请输入工程名称：", "新工程");
        if (string.IsNullOrWhiteSpace(name)) return;

        await _projectService.CreateNewProjectAsync(name);
        StatusText = $"已创建工程：{name}";
    }

    /// <summary>
    /// 打开工程命令。若当前工程有未保存修改，先询问是否保存，再弹出文件选择对话框。
    /// </summary>
    [RelayCommand]
    private async Task OpenProject()
    {
        if (_projectService.IsDirty)
        {
            var save = await _dialogService.ShowConfirmAsync("保存", "当前工程已修改，是否保存？");
            if (save) await SaveProject();
        }

        var path = await _dialogService.OpenFileAsync("打开工程",
            [new FileFilter("HMI工程文件", ["hmiproj", "json"])]);
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            await _projectService.OpenProjectAsync(path);
            StatusText = $"已打开：{path}";
            RecentProjects.Clear();
            foreach (var r in _projectService.RecentProjects)
                RecentProjects.Add(r);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("错误", $"打开工程失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 保存工程命令。若工程尚未关联文件路径，则自动转为"另存为"流程。
    /// </summary>
    [RelayCommand]
    private async Task SaveProject()
    {
        if (_projectService.CurrentProject == null) return;

        try
        {
            await _projectService.SaveProjectAsync();
            StatusText = "保存成功";
        }
        catch (InvalidOperationException)
        {
            // 工程没有关联路径时，ServiceLayer 抛出此异常，转向另存为
            await SaveProjectAs();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("错误", $"保存失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 另存为命令。弹出文件保存对话框让用户指定新路径后保存。
    /// </summary>
    [RelayCommand]
    private async Task SaveProjectAs()
    {
        if (_projectService.CurrentProject == null) return;

        var path = await _dialogService.SaveFileAsync("另存为",
            [new FileFilter("HMI工程文件", ["hmiproj"])],
            _projectService.CurrentProject.Name);
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            await _projectService.SaveProjectAsAsync(path);
            StatusText = $"已保存至：{path}";
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("错误", $"保存失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 撤销命令，执行设计器的上一步操作。
    /// </summary>
    [RelayCommand]
    private void Undo()
    {
        DesignerViewModel?.UndoRedo.Undo();
        StatusText = "已撤销";
    }

    /// <summary>
    /// 重做命令，重新执行已撤销的操作。
    /// </summary>
    [RelayCommand]
    private void Redo()
    {
        DesignerViewModel?.UndoRedo.Redo();
        StatusText = "已重做";
    }

    /// <summary>
    /// 切换主内容区域为设计器视图。
    /// </summary>
    [RelayCommand]
    private void ShowDesigner()
    {
        CurrentContent = DesignerViewModel;
        StatusText = "设计模式";
    }

    /// <summary>切换主内容区域为变量管理器视图。</summary>
    [RelayCommand]
    private void ShowVariableManager() => CurrentContent = VariableManager;

    /// <summary>切换主内容区域为脚本编辑器视图。</summary>
    [RelayCommand]
    private void ShowScriptEditor() => CurrentContent = ScriptEditor;

    /// <summary>切换主内容区域为报警管理器视图。</summary>
    [RelayCommand]
    private void ShowAlarmManager() => CurrentContent = AlarmManager;

    /// <summary>切换主内容区域为通信管理器视图。</summary>
    [RelayCommand]
    private void ShowCommunicationManager() => CurrentContent = CommunicationManager;

    /// <summary>切换主内容区域为资源管理器视图。</summary>
    [RelayCommand]
    private void ShowResourceManager() => CurrentContent = ResourceManager;

    /// <summary>
    /// 切换主内容区域为运行时预览视图。
    /// 将当前设计器的页面和工程变量传递给运行时 ViewModel 以初始化绑定。
    /// </summary>
    [RelayCommand]
    private void ShowRuntimePreview()
    {
        if (DesignerViewModel == null) return;
        RuntimePreview.LoadPages(
            DesignerViewModel.Pages,
            _projectService.CurrentProject?.Variables ?? []);
        CurrentContent = RuntimePreview;
        StatusText = "预览运行中";
    }

    /// <summary>
    /// 切换设计器网格线的显示/隐藏状态。
    /// </summary>
    [RelayCommand]
    private void ToggleGrid()
    {
        if (DesignerViewModel != null)
            DesignerViewModel.ShowGrid = !DesignerViewModel.ShowGrid;
    }

    /// <summary>
    /// 在深色主题与浅色主题之间切换，并立即应用到 Avalonia 应用程序。
    /// </summary>
    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        if (Avalonia.Application.Current != null)
        {
            Avalonia.Application.Current.RequestedThemeVariant =
                IsDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
        }
        StatusText = IsDarkTheme ? "已切换为深色主题" : "已切换为浅色主题";
    }

    /// <summary>
    /// 退出应用程序命令。若工程有未保存修改，先询问是否保存，再关闭应用。
    /// </summary>
    [RelayCommand]
    private async Task ExitApplication()
    {
        if (_projectService.IsDirty)
        {
            var save = await _dialogService.ShowConfirmAsync("退出", "工程已修改，退出前是否保存？");
            if (save) await SaveProject();
        }
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    /// <summary>
    /// 显示"关于"对话框，展示软件版本和功能介绍信息。
    /// </summary>
    [RelayCommand]
    private async Task ShowAbout()
    {
        await _dialogService.ShowMessageAsync("关于 HMI Designer",
            "HMI组态软件 v1.0\n基于 .NET 8 + Avalonia UI\n\n支持拖放设计、脚本编辑、变量管理、通信配置和报警管理。");
    }
}
