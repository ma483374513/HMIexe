/// <summary>
/// 运行时预览视图模型文件。
/// 在设计模式下模拟 HMI 画面运行效果：将变量值绑定到控件属性，
/// 并实时响应变量值变化以更新控件显示状态。
/// </summary>
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HMIexe.Core.Models.Canvas;
using HMIexe.Core.Models.Controls;
using HMIexe.Core.Models.Variables;
using HMIexe.Core.Services;

namespace HMIexe.App.ViewModels;

/// <summary>
/// 运行时预览视图模型。
/// 将设计器中的页面加载到预览模式，建立变量到控件属性的单向绑定，
/// 并通过订阅 <see cref="IVariableService.VariableValueChanged"/> 事件实时刷新控件值。
/// </summary>
public partial class RuntimeViewModel : ObservableObject
{
    /// <summary>变量业务服务，用于读取初始变量值和监听变量变化事件。</summary>
    private readonly IVariableService _variableService;

    /// <summary>当前在预览区域显示的 HMI 页面。</summary>
    [ObservableProperty]
    private HmiPage? _currentPage;

    /// <summary>预览画布的缩放级别，范围 0.1～4.0。</summary>
    [ObservableProperty]
    private double _zoomLevel = 1.0;

    /// <summary>预览模式的状态文本，绑定到底部状态栏。</summary>
    [ObservableProperty]
    private string _statusText = "预览运行中";

    /// <summary>已加载到预览模式的所有 HMI 页面集合，绑定到页面导航列表。</summary>
    public ObservableCollection<HmiPage> Pages { get; } = new();

    /// <summary>
    /// 初始化运行时预览视图模型。订阅变量值变化事件以实时刷新控件。
    /// </summary>
    /// <param name="variableService">变量业务服务。</param>
    public RuntimeViewModel(IVariableService variableService)
    {
        _variableService = variableService;
        _variableService.VariableValueChanged += OnVariableValueChanged;
    }

    /// <summary>
    /// 加载页面集合到预览模式，并对当前页所有控件应用变量初始值绑定。
    /// </summary>
    /// <param name="pages">来自设计器的 HMI 页面集合。</param>
    /// <param name="variables">工程变量集合（当前由服务内部维护，此参数保留以备扩展）。</param>
    public void LoadPages(IEnumerable<HmiPage> pages, IEnumerable<HmiVariable> variables)
    {
        Pages.Clear();
        foreach (var p in pages)
            Pages.Add(p);
        CurrentPage = Pages.FirstOrDefault();

        // 利用已注册到服务的变量初始值为第一个页面的控件赋值
        if (CurrentPage != null)
            ApplyBindings(CurrentPage);
    }

    /// <summary>
    /// 切换预览页面命令，切换后立即为新页面的所有控件应用当前变量值。
    /// </summary>
    /// <param name="page">要切换到的目标页面。</param>
    [RelayCommand]
    private void SelectPage(HmiPage page)
    {
        CurrentPage = page;
        if (page != null)
            ApplyBindings(page);
    }

    /// <summary>放大预览画布，最大缩放比例为 400%。</summary>
    [RelayCommand]
    private void ZoomIn() => ZoomLevel = Math.Min(ZoomLevel + 0.1, 4.0);

    /// <summary>缩小预览画布，最小缩放比例为 10%。</summary>
    [RelayCommand]
    private void ZoomOut() => ZoomLevel = Math.Max(ZoomLevel - 0.1, 0.1);

    /// <summary>重置预览画布缩放为 100%。</summary>
    [RelayCommand]
    private void ResetZoom() => ZoomLevel = 1.0;

    /// <summary>
    /// 变量值变化事件处理器。
    /// 仅对当前显示页面中绑定了该变量的控件更新其属性值。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">包含变量名和新值的事件参数。</param>
    private void OnVariableValueChanged(object? sender, VariableValueChangedEventArgs e)
    {
        if (CurrentPage == null) return;
        foreach (var ctrl in CurrentPage.AllControls)
        {
            // 只更新绑定了本变量的控件
            if (ctrl.ValueBindingVariable == e.VariableName)
                ApplyValueToControl(ctrl, e.NewValue);
        }
    }

    /// <summary>
    /// 遍历指定页面的所有控件，根据其绑定变量名从服务读取当前值并应用。
    /// </summary>
    /// <param name="page">要初始化绑定的目标页面。</param>
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

    /// <summary>
    /// 将给定值应用到控件的对应属性。
    /// 根据控件类型进行类型转换后赋值；转换失败时静默忽略，不中断预览。
    /// </summary>
    /// <param name="ctrl">目标控件。</param>
    /// <param name="value">要应用的变量值（可为 <c>null</c>）。</param>
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
        catch (InvalidCastException)
        {
            // 忽略变量类型与控件值类型不兼容的转换错误
        }
        catch (FormatException)
        {
            // 忽略将字符串变量值转换为数值类型时的格式错误
        }
        catch (OverflowException)
        {
            // 忽略数值转换时的溢出错误
        }
    }
}
