/// <summary>
/// 工程发布视图模型文件。
/// 负责收集用户配置的发布参数，驱动 <see cref="IPublishService"/> 执行打包，
/// 并将进度日志实时呈现到 UI。
/// </summary>
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HMIexe.App.Services;
using HMIexe.Core.Models.Project;
using HMIexe.Core.Services;

namespace HMIexe.App.ViewModels;

/// <summary>
/// 工程发布视图模型。
/// 提供平台选择、分辨率设置、资源优化、脚本模式等发布参数的双向绑定，
/// 以及执行发布、取消发布和浏览输出目录等命令。
/// </summary>
public partial class PublishViewModel : ObservableObject
{
    /// <summary>工程服务，提供当前已打开的工程模型。</summary>
    private readonly IProjectService _projectService;

    /// <summary>发布服务，执行实际的打包和编译操作。</summary>
    private readonly IPublishService _publishService;

    /// <summary>对话框服务，用于文件夹选择和消息提示。</summary>
    private readonly IDialogService _dialogService;

    /// <summary>当前正在运行的发布任务的取消令牌源；未发布时为 null。</summary>
    private CancellationTokenSource? _publishCts;

    // ── 发布参数属性 ──────────────────────────────────────────────────

    /// <summary>目标发布平台列表，供下拉框绑定使用。</summary>
    public IReadOnlyList<PublishPlatform> AvailablePlatforms { get; } =
        Enum.GetValues<PublishPlatform>();

    /// <summary>脚本编译模式列表，供下拉框绑定使用。</summary>
    public IReadOnlyList<ScriptMode> AvailableScriptModes { get; } =
        Enum.GetValues<ScriptMode>();

    /// <summary>当前选择的目标发布平台。</summary>
    [ObservableProperty]
    private PublishPlatform _selectedPlatform = PublishPlatform.WindowsX64;

    /// <summary>发布目标画面宽度（像素）。</summary>
    [ObservableProperty]
    private int _resolutionWidth = 1920;

    /// <summary>发布目标画面高度（像素）。</summary>
    [ObservableProperty]
    private int _resolutionHeight = 1080;

    /// <summary>是否启用资源加载优化（压缩/合并资源文件）。</summary>
    [ObservableProperty]
    private bool _optimizeResources = true;

    /// <summary>当前选择的脚本编译模式。</summary>
    [ObservableProperty]
    private ScriptMode _selectedScriptMode = ScriptMode.Release;

    /// <summary>是否将所有文件合并为单一可执行文件（Self-contained 单文件发布）。</summary>
    [ObservableProperty]
    private bool _singleFilePublish = false;

    /// <summary>发布输出目录路径；为空时由发布服务自动选择桌面目录。</summary>
    [ObservableProperty]
    private string _outputDirectory = string.Empty;

    // ── 状态属性 ──────────────────────────────────────────────────────

    /// <summary>是否正在执行发布操作，用于控制按钮可用性和进度指示器显示。</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PublishCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelPublishCommand))]
    private bool _isPublishing;

    /// <summary>底部状态栏文本，显示最近一次操作的简短描述。</summary>
    [ObservableProperty]
    private string _statusText = "就绪";

    /// <summary>发布操作的日志输出列表，实时追加进度信息。</summary>
    public ObservableCollection<string> LogLines { get; } = new();

    /// <summary>
    /// 初始化发布视图模型并注入所需服务。
    /// </summary>
    public PublishViewModel(
        IProjectService projectService,
        IPublishService publishService,
        IDialogService dialogService)
    {
        _projectService = projectService;
        _publishService = publishService;
        _dialogService  = dialogService;
    }

    // ── 命令 ──────────────────────────────────────────────────────────

    /// <summary>
    /// 浏览并选择发布输出目录。
    /// </summary>
    [RelayCommand]
    private async Task BrowseOutputDirectory()
    {
        var path = await _dialogService.SaveFileAsync(
            "选择发布输出目录（在此目录下创建发布文件夹）",
            [new FileFilter("所有文件", ["*"])],
            "publish_output");
        if (!string.IsNullOrEmpty(path))
            OutputDirectory = Path.GetDirectoryName(path) ?? path;
    }

    /// <summary>
    /// 执行工程发布命令。
    /// 将当前参数传入 <see cref="IPublishService"/>，并将进度日志输出到 <see cref="LogLines"/>。
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanPublish))]
    private async Task Publish()
    {
        var project = _projectService.CurrentProject;
        if (project == null)
        {
            await _dialogService.ShowMessageAsync("发布", "请先打开或新建一个工程，然后再发布。");
            return;
        }

        LogLines.Clear();
        IsPublishing = true;
        StatusText   = "正在发布…";

        _publishCts = new CancellationTokenSource();
        var progress = new Progress<string>(line =>
        {
            LogLines.Add(line);
        });

        try
        {
            var settings = BuildSettings();
            var outputPath = await _publishService.PublishAsync(
                project, settings, progress, _publishCts.Token);
            StatusText = $"发布成功：{outputPath}";
            await _dialogService.ShowMessageAsync("发布完成",
                $"工程已发布至：\n{outputPath}");
        }
        catch (OperationCanceledException)
        {
            LogLines.Add("⚠ 发布已取消。");
            StatusText = "发布已取消";
        }
        catch (Exception ex)
        {
            LogLines.Add($"❌ 发布失败：{ex.Message}");
            StatusText = "发布失败";
            await _dialogService.ShowMessageAsync("发布失败", ex.Message);
        }
        finally
        {
            IsPublishing = false;
            _publishCts.Dispose();
            _publishCts = null;
        }
    }

    private bool CanPublish() => !IsPublishing;

    /// <summary>
    /// 取消正在执行的发布操作。
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCancelPublish))]
    private void CancelPublish()
    {
        _publishCts?.Cancel();
        StatusText = "正在取消…";
    }

    private bool CanCancelPublish() => IsPublishing;

    /// <summary>
    /// 清空日志输出区域。
    /// </summary>
    [RelayCommand]
    private void ClearLog() => LogLines.Clear();

    /// <summary>
    /// 快速应用分辨率预设。
    /// <paramref name="preset"/> 格式为 "宽x高"，例如 "1920x1080"。
    /// </summary>
    [RelayCommand]
    private void ApplyPreset(string preset)
    {
        var parts = preset.Split('x');
        if (parts.Length == 2
            && int.TryParse(parts[0], out var w)
            && int.TryParse(parts[1], out var h))
        {
            ResolutionWidth  = w;
            ResolutionHeight = h;
        }
    }

    // ── 私有辅助 ──────────────────────────────────────────────────────

    /// <summary>
    /// 根据当前 ViewModel 属性值构建 <see cref="PublishSettings"/> 对象。
    /// </summary>
    private PublishSettings BuildSettings() => new()
    {
        Platform          = SelectedPlatform,
        ResolutionWidth   = ResolutionWidth,
        ResolutionHeight  = ResolutionHeight,
        OptimizeResources = OptimizeResources,
        ScriptMode        = SelectedScriptMode,
        SingleFilePublish = SingleFilePublish,
        OutputDirectory   = OutputDirectory
    };
}
