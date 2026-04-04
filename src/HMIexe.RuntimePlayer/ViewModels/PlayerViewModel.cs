/// <summary>
/// 运行画面播放器视图模型文件。
/// 负责从文件加载工程，初始化变量，提供页面导航、缩放和变量绑定。
/// </summary>
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HMIexe.Core.Models.Canvas;
using HMIexe.Core.Models.Controls;
using HMIexe.Core.Models.Variables;
using HMIexe.Core.Services;

namespace HMIexe.RuntimePlayer.ViewModels;

/// <summary>
/// 运行画面播放器视图模型。
/// 从 <c>project.hmiproj</c> 文件加载 HMI 工程，建立变量→控件的单向绑定，
/// 并通过订阅 <see cref="IVariableService.VariableValueChanged"/> 实时刷新控件值。
/// </summary>
public partial class PlayerViewModel : ObservableObject
{
    private readonly IProjectService _projectService;
    private readonly IVariableService _variableService;

    /// <summary>当前显示的页面。</summary>
    [ObservableProperty]
    private HmiPage? _currentPage;

    /// <summary>画布缩放级别，范围 0.1～4.0。</summary>
    [ObservableProperty]
    private double _zoomLevel = 1.0;

    /// <summary>状态栏文本。</summary>
    [ObservableProperty]
    private string _statusText = "正在加载工程…";

    /// <summary>窗口标题（包含工程名）。</summary>
    [ObservableProperty]
    private string _title = "HMI 运行画面播放器";

    /// <summary>是否正在加载，用于控制加载提示显示。</summary>
    [ObservableProperty]
    private bool _isLoading = true;

    /// <summary>加载失败时的错误信息；加载成功时为空。</summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>工程中所有页面的集合。</summary>
    public ObservableCollection<HmiPage> Pages { get; } = new();

    /// <summary>
    /// 初始化播放器视图模型。
    /// </summary>
    public PlayerViewModel(IProjectService projectService, IVariableService variableService)
    {
        _projectService = projectService;
        _variableService = variableService;
        _variableService.VariableValueChanged += OnVariableValueChanged;
    }

    /// <summary>
    /// 从文件加载工程并初始化变量绑定。
    /// </summary>
    /// <param name="projectFilePath">工程文件路径；为 null 时显示错误提示。</param>
    public async Task LoadProjectAsync(string? projectFilePath)
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            if (string.IsNullOrEmpty(projectFilePath) || !File.Exists(projectFilePath))
            {
                ErrorMessage = projectFilePath == null
                    ? "未找到工程文件。\n\n请将 project.hmiproj 放置到播放器同目录，\n或通过命令行参数指定工程文件路径：\n  HMIexe.Player <path/to/project.hmiproj>"
                    : $"工程文件不存在：{projectFilePath}";
                StatusText = "加载失败";
                return;
            }

            var project = await _projectService.OpenProjectAsync(projectFilePath);

            // 将工程变量注册到变量服务
            foreach (var variable in project.Variables)
                _variableService.AddVariable(variable);

            // 加载页面集合
            Pages.Clear();
            foreach (var page in project.Pages)
                Pages.Add(page);

            // 选择默认页面
            var defaultPage = Pages.FirstOrDefault(p => p.Id == project.DefaultPageId)
                              ?? Pages.FirstOrDefault();
            CurrentPage = defaultPage;

            if (CurrentPage != null)
                ApplyBindings(CurrentPage);

            Title = $"{project.Name} — HMI 运行画面播放器";
            StatusText = $"已加载工程：{project.Name}，共 {Pages.Count} 个画面";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"加载工程失败：{ex.Message}";
            StatusText = "加载失败";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>切换到指定页面。</summary>
    [RelayCommand]
    private void SelectPage(HmiPage page)
    {
        CurrentPage = page;
        if (page != null)
            ApplyBindings(page);
    }

    /// <summary>放大画布，最大 400%。</summary>
    [RelayCommand]
    private void ZoomIn() => ZoomLevel = Math.Min(ZoomLevel + 0.1, 4.0);

    /// <summary>缩小画布，最小 10%。</summary>
    [RelayCommand]
    private void ZoomOut() => ZoomLevel = Math.Max(ZoomLevel - 0.1, 0.1);

    /// <summary>重置缩放为 100%。</summary>
    [RelayCommand]
    private void ResetZoom() => ZoomLevel = 1.0;

    // ── 私有方法 ────────────────────────────────────────────────────

    private void OnVariableValueChanged(object? sender, VariableValueChangedEventArgs e)
    {
        if (CurrentPage == null) return;
        foreach (var ctrl in CurrentPage.AllControls)
        {
            if (ctrl.ValueBindingVariable == e.VariableName)
                ApplyValueToControl(ctrl, e.NewValue);
        }
    }

    private void ApplyBindings(HmiPage page)
    {
        foreach (var ctrl in page.AllControls)
        {
            if (string.IsNullOrEmpty(ctrl.ValueBindingVariable)) continue;
            var variable = _variableService.GetVariableByName(ctrl.ValueBindingVariable);
            if (variable != null)
                ApplyValueToControl(ctrl, variable.Value);
        }
    }

    private static void ApplyValueToControl(HmiControlBase ctrl, object? value)
    {
        try
        {
            switch (ctrl)
            {
                case GaugeControl g:
                    g.Value = Convert.ToDouble(value ?? 0);
                    break;
                case SliderControl s:
                    s.Value = Convert.ToDouble(value ?? 0);
                    break;
                case IndicatorLightControl light:
                    light.IsOn = Convert.ToBoolean(value ?? false);
                    break;
                case SwitchControl sw:
                    sw.IsOn = Convert.ToBoolean(value ?? false);
                    break;
                case LabelControl lbl:
                    lbl.Text = value?.ToString() ?? string.Empty;
                    break;
                case TextBoxControl tb:
                    tb.Text = value?.ToString() ?? string.Empty;
                    break;
            }
        }
        catch (InvalidCastException) { }
        catch (FormatException) { }
        catch (OverflowException) { }
    }
}
