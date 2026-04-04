/// <summary>
/// 属性面板视图模型文件。
/// 通过统一的属性代理为不同类型的 HMI 控件提供属性编辑能力，
/// 所有属性修改均通过撤销/重做系统记录，支持多步撤销。
/// </summary>
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using HMIexe.Core.Models.Canvas;
using HMIexe.Core.Models.Controls;
using HMIexe.Core.Services;
using HMIexe.Core.UndoRedo;

namespace HMIexe.App.ViewModels;

/// <summary>
/// 属性面板视图模型。
/// 作为设计器属性面板的数据源，将选中控件的各项属性通过代理属性暴露给视图绑定。
/// 每次属性写入均封装为 <see cref="SetPropertyAction"/> 并提交到
/// <see cref="UndoRedoHistory"/>，以支持撤销/重做。
/// </summary>
public partial class PropertyPanelViewModel : ObservableObject
{
    /// <summary>撤销/重做历史记录管理器，由设计器视图模型共享传入。</summary>
    private readonly UndoRedoHistory _undoRedo;

    /// <summary>变量服务，用于获取可绑定变量名列表（可为 null，表示不支持变量绑定）。</summary>
    private readonly IVariableService? _variableService;

    /// <summary>当前选中的控件；变更时触发所有代理属性的 PropertyChanged 通知。</summary>
    [ObservableProperty]
    private HmiControlBase? _selectedControl;

    /// <summary>当前编辑的 HMI 页面，供属性面板扩展使用。</summary>
    [ObservableProperty]
    private HmiPage? _currentPage;

    /// <summary>属性搜索关键字（预留，用于过滤属性列表）。</summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>All variable names available for binding (populated from IVariableService).</summary>
    public ObservableCollection<string> AvailableVariables { get; } = new() { string.Empty };

    /// <summary>
    /// 初始化属性面板视图模型。
    /// </summary>
    /// <param name="undoRedo">共享的撤销/重做历史记录管理器。</param>
    /// <param name="variableService">变量服务，可选；提供时自动加载变量名列表。</param>
    public PropertyPanelViewModel(UndoRedoHistory undoRedo, IVariableService? variableService = null)
    {
        _undoRedo = undoRedo;
        _variableService = variableService;
        if (_variableService != null)
            RefreshVariableList();
    }

    /// <summary>
    /// 刷新可绑定变量名列表。清空并重新从变量服务加载所有变量名。
    /// 列表首项保留空字符串，表示不绑定任何变量。
    /// </summary>
    public void RefreshVariableList()
    {
        AvailableVariables.Clear();
        AvailableVariables.Add(string.Empty);
        if (_variableService == null) return;
        foreach (var v in _variableService.Variables)
            AvailableVariables.Add(v.Name);
    }

    /// <summary>
    /// 选中控件变更时的响应方法。
    /// 若变量列表尚未加载则触发加载，并逐一通知所有代理属性刷新，
    /// 确保视图中的属性值与新选中控件保持同步。
    /// </summary>
    partial void OnSelectedControlChanged(HmiControlBase? value)
    {
        // Only refresh variable list if it might be stale (do not rebuild on every selection)
        if (AvailableVariables.Count == 1 && _variableService?.Variables.Count > 0)
            RefreshVariableList();
        OnPropertyChanged(nameof(ControlName));
        OnPropertyChanged(nameof(ControlX));
        OnPropertyChanged(nameof(ControlY));
        OnPropertyChanged(nameof(ControlWidth));
        OnPropertyChanged(nameof(ControlHeight));
        OnPropertyChanged(nameof(ControlVisible));
        OnPropertyChanged(nameof(ControlLocked));
        OnPropertyChanged(nameof(ControlOpacity));
        OnPropertyChanged(nameof(ControlZIndex));
        OnPropertyChanged(nameof(ControlText));
        OnPropertyChanged(nameof(HasControlText));
        OnPropertyChanged(nameof(HasSelectedControl));
        OnPropertyChanged(nameof(ControlValueVariable));
        // Control-specific
        OnPropertyChanged(nameof(IsButtonControl));
        OnPropertyChanged(nameof(IsLabelControl));
        OnPropertyChanged(nameof(IsGaugeControl));
        OnPropertyChanged(nameof(IsIndicatorLightControl));
        OnPropertyChanged(nameof(IsSwitchControl));
        OnPropertyChanged(nameof(IsSliderControl));
        OnPropertyChanged(nameof(IsShapeControl));
        OnPropertyChanged(nameof(ControlBackgroundColor));
        OnPropertyChanged(nameof(ControlForegroundColor));
        OnPropertyChanged(nameof(ControlFillColor));
        OnPropertyChanged(nameof(ControlStrokeColor));
        OnPropertyChanged(nameof(ControlStrokeThickness));
        OnPropertyChanged(nameof(GaugeValue));
        OnPropertyChanged(nameof(GaugeMinValue));
        OnPropertyChanged(nameof(GaugeMaxValue));
        OnPropertyChanged(nameof(GaugeUnit));
        OnPropertyChanged(nameof(LightIsOn));
        OnPropertyChanged(nameof(LightOnColor));
        OnPropertyChanged(nameof(LightOffColor));
        OnPropertyChanged(nameof(SwitchIsOn));
        OnPropertyChanged(nameof(SwitchOnLabel));
        OnPropertyChanged(nameof(SwitchOffLabel));
        OnPropertyChanged(nameof(SliderValue));
        OnPropertyChanged(nameof(SliderMinimum));
        OnPropertyChanged(nameof(SliderMaximum));
        OnPropertyChanged(nameof(LabelFontSize));
        OnPropertyChanged(nameof(CornerRadius));
    }

    /// <summary>是否有控件处于选中状态，用于控制属性面板的可用性。</summary>
    public bool HasSelectedControl => SelectedControl != null;

    /// <summary>True for controls that have an editable Text/Label property.</summary>
    public bool HasControlText => SelectedControl is ButtonControl
        or LabelControl or TextBoxControl;

    // ── 控件类型判断属性 ──────────────────────────────────────────────────
    /// <summary>是否为按钮控件，控制按钮专属属性区域的显示。</summary>
    public bool IsButtonControl => SelectedControl is ButtonControl;
    /// <summary>是否为标签控件，控制标签专属属性区域的显示。</summary>
    public bool IsLabelControl => SelectedControl is LabelControl;
    /// <summary>是否为仪表控件，控制仪表专属属性区域的显示。</summary>
    public bool IsGaugeControl => SelectedControl is GaugeControl;
    /// <summary>是否为指示灯控件，控制指示灯专属属性区域的显示。</summary>
    public bool IsIndicatorLightControl => SelectedControl is IndicatorLightControl;
    /// <summary>是否为开关控件，控制开关专属属性区域的显示。</summary>
    public bool IsSwitchControl => SelectedControl is SwitchControl;
    /// <summary>是否为滑动条控件，控制滑动条专属属性区域的显示。</summary>
    public bool IsSliderControl => SelectedControl is SliderControl;
    /// <summary>是否为形状控件（矩形、圆形、椭圆或直线），控制形状专属属性区域的显示。</summary>
    public bool IsShapeControl => SelectedControl is RectangleControl
        or CircleControl or EllipseControl or LineControl;

    // ── 变量绑定属性 ─────────────────────────────────────────────
    /// <summary>
    /// 控件绑定的变量名称。读取或设置绑定变量时通过撤销/重做系统记录操作。
    /// </summary>
    public string ControlValueVariable
    {
        get => SelectedControl?.ValueBindingVariable ?? string.Empty;
        set
        {
            if (SelectedControl == null) return;
            var old = SelectedControl.ValueBindingVariable;
            if (old == value) return;
            _undoRedo.Execute(new SetPropertyAction("绑定变量",
                () => SelectedControl.ValueBindingVariable = value,
                () => SelectedControl.ValueBindingVariable = old));
            OnPropertyChanged();
        }
    }

    // ── 文本属性 ─────────────────────────────────────────────────────────
    /// <summary>
    /// 控件的文本内容代理属性。
    /// 对按钮返回/设置按钮文本，对标签返回/设置标签文本，
    /// 对输入框返回/设置占位符文本。修改通过撤销/重做系统记录。
    /// </summary>
    public string ControlText
    {
        get => SelectedControl switch
        {
            ButtonControl b => b.Text,
            LabelControl l => l.Text,
            TextBoxControl t => t.Placeholder,
            _ => string.Empty
        };
        set
        {
            if (SelectedControl == null) return;
            switch (SelectedControl)
            {
                case ButtonControl b:
                    var oldB = b.Text;
                    if (oldB == value) return;
                    _undoRedo.Execute(new SetPropertyAction("修改按钮文本",
                        () => b.Text = value, () => b.Text = oldB));
                    break;
                case LabelControl l:
                    var oldL = l.Text;
                    if (oldL == value) return;
                    _undoRedo.Execute(new SetPropertyAction("修改标签文本",
                        () => l.Text = value, () => l.Text = oldL));
                    break;
                case TextBoxControl t:
                    var oldT = t.Placeholder;
                    if (oldT == value) return;
                    _undoRedo.Execute(new SetPropertyAction("修改输入框占位符",
                        () => t.Placeholder = value, () => t.Placeholder = oldT));
                    break;
            }
            OnPropertyChanged();
        }
    }

    // ── 颜色属性 ───────────────────────────────────────────────────────
    /// <summary>
    /// 控件背景颜色代理属性（仅对按钮控件有效）。
    /// 颜色值为十六进制字符串，例如 "#FF0000"。修改通过撤销/重做系统记录。
    /// </summary>
    public string ControlBackgroundColor
    {
        get => (SelectedControl as ButtonControl)?.BackgroundColor ?? string.Empty;
        set
        {
            if (SelectedControl is not ButtonControl b) return;
            var old = b.BackgroundColor;
            if (old == value) return;
            _undoRedo.Execute(new SetPropertyAction("修改背景色",
                () => b.BackgroundColor = value, () => b.BackgroundColor = old));
            OnPropertyChanged();
        }
    }

    /// <summary>控件前景色代理属性（按钮文字颜色或标签文字颜色）。修改通过撤销/重做系统记录。</summary>
    public string ControlForegroundColor
    {
        get => SelectedControl switch
        {
            ButtonControl b => b.ForegroundColor,
            LabelControl l => l.ForegroundColor,
            _ => string.Empty
        };
        set
        {
            if (SelectedControl == null) return;
            switch (SelectedControl)
            {
                case ButtonControl b:
                    var oldB = b.ForegroundColor;
                    if (oldB == value) return;
                    _undoRedo.Execute(new SetPropertyAction("修改前景色",
                        () => b.ForegroundColor = value, () => b.ForegroundColor = oldB));
                    break;
                case LabelControl l:
                    var oldL = l.ForegroundColor;
                    if (oldL == value) return;
                    _undoRedo.Execute(new SetPropertyAction("修改文字颜色",
                        () => l.ForegroundColor = value, () => l.ForegroundColor = oldL));
                    break;
            }
            OnPropertyChanged();
        }
    }

    /// <summary>形状控件填充色代理属性（矩形、圆形、椭圆）。修改通过撤销/重做系统记录。</summary>
    public string ControlFillColor
    {
        get => SelectedControl switch
        {
            RectangleControl r => r.FillColor,
            CircleControl c => c.FillColor,
            EllipseControl e => e.FillColor,
            _ => string.Empty
        };
        set
        {
            if (SelectedControl == null) return;
            switch (SelectedControl)
            {
                case RectangleControl r:
                    var oldR = r.FillColor;
                    if (oldR == value) return;
                    _undoRedo.Execute(new SetPropertyAction("修改填充色",
                        () => r.FillColor = value, () => r.FillColor = oldR));
                    break;
                case CircleControl c:
                    var oldC = c.FillColor;
                    if (oldC == value) return;
                    _undoRedo.Execute(new SetPropertyAction("修改填充色",
                        () => c.FillColor = value, () => c.FillColor = oldC));
                    break;
                case EllipseControl e:
                    var oldE = e.FillColor;
                    if (oldE == value) return;
                    _undoRedo.Execute(new SetPropertyAction("修改填充色",
                        () => e.FillColor = value, () => e.FillColor = oldE));
                    break;
            }
            OnPropertyChanged();
        }
    }

    /// <summary>形状控件描边颜色代理属性（矩形、圆形、椭圆、直线）。修改通过撤销/重做系统记录。</summary>
    public string ControlStrokeColor
    {
        get => SelectedControl switch
        {
            RectangleControl r => r.StrokeColor,
            CircleControl c => c.StrokeColor,
            EllipseControl e => e.StrokeColor,
            LineControl l => l.StrokeColor,
            _ => string.Empty
        };
        set
        {
            if (SelectedControl == null) return;
            string old = string.Empty;
            switch (SelectedControl)
            {
                case RectangleControl r:
                    old = r.StrokeColor;
                    if (old == value) return;
                    _undoRedo.Execute(new SetPropertyAction("修改描边色",
                        () => r.StrokeColor = value, () => r.StrokeColor = old));
                    break;
                case CircleControl c:
                    old = c.StrokeColor;
                    if (old == value) return;
                    _undoRedo.Execute(new SetPropertyAction("修改描边色",
                        () => c.StrokeColor = value, () => c.StrokeColor = old));
                    break;
                case EllipseControl e:
                    old = e.StrokeColor;
                    if (old == value) return;
                    _undoRedo.Execute(new SetPropertyAction("修改描边色",
                        () => e.StrokeColor = value, () => e.StrokeColor = old));
                    break;
                case LineControl l:
                    old = l.StrokeColor;
                    if (old == value) return;
                    _undoRedo.Execute(new SetPropertyAction("修改线条颜色",
                        () => l.StrokeColor = value, () => l.StrokeColor = old));
                    break;
            }
            OnPropertyChanged();
        }
    }

    /// <summary>形状控件描边宽度代理属性（最小值为 0）。修改通过撤销/重做系统记录。</summary>
    public double ControlStrokeThickness
    {
        get => SelectedControl switch
        {
            RectangleControl r => r.StrokeThickness,
            CircleControl c => c.StrokeThickness,
            EllipseControl e => e.StrokeThickness,
            LineControl l => l.StrokeThickness,
            _ => 1
        };
        set
        {
            if (SelectedControl == null || value < 0) return;
            switch (SelectedControl)
            {
                case RectangleControl r:
                    var oldR = r.StrokeThickness;
                    if (Math.Abs(oldR - value) < 0.001) return;
                    _undoRedo.Execute(new SetPropertyAction("修改描边宽度",
                        () => r.StrokeThickness = value, () => r.StrokeThickness = oldR));
                    break;
                case CircleControl c:
                    var oldC = c.StrokeThickness;
                    if (Math.Abs(oldC - value) < 0.001) return;
                    _undoRedo.Execute(new SetPropertyAction("修改描边宽度",
                        () => c.StrokeThickness = value, () => c.StrokeThickness = oldC));
                    break;
                case EllipseControl e:
                    var oldE = e.StrokeThickness;
                    if (Math.Abs(oldE - value) < 0.001) return;
                    _undoRedo.Execute(new SetPropertyAction("修改描边宽度",
                        () => e.StrokeThickness = value, () => e.StrokeThickness = oldE));
                    break;
                case LineControl l:
                    var oldL = l.StrokeThickness;
                    if (Math.Abs(oldL - value) < 0.001) return;
                    _undoRedo.Execute(new SetPropertyAction("修改线宽",
                        () => l.StrokeThickness = value, () => l.StrokeThickness = oldL));
                    break;
            }
            OnPropertyChanged();
        }
    }

    /// <summary>矩形控件圆角半径代理属性（最小值为 0，0 表示直角）。修改通过撤销/重做系统记录。</summary>
    public double CornerRadius
    {
        get => (SelectedControl as RectangleControl)?.CornerRadius ?? 0;
        set
        {
            if (SelectedControl is not RectangleControl r || value < 0) return;
            var old = r.CornerRadius;
            if (Math.Abs(old - value) < 0.001) return;
            _undoRedo.Execute(new SetPropertyAction("修改圆角",
                () => r.CornerRadius = value, () => r.CornerRadius = old));
            OnPropertyChanged();
        }
    }

    // ── 仪表专属属性 ────────────────────────────────────────────────────
    /// <summary>
    /// 仪表控件当前显示值的代理属性。修改通过撤销/重做系统记录。
    /// </summary>
    public double GaugeValue
    {
        get => (SelectedControl as GaugeControl)?.Value ?? 0;
        set
        {
            if (SelectedControl is not GaugeControl g) return;
            var old = g.Value;
            if (Math.Abs(old - value) < 0.001) return;
            _undoRedo.Execute(new SetPropertyAction("修改仪表值",
                () => g.Value = value, () => g.Value = old));
            OnPropertyChanged();
        }
    }

    /// <summary>仪表控件量程最小值代理属性。修改通过撤销/重做系统记录。</summary>
    public double GaugeMinValue
    {
        get => (SelectedControl as GaugeControl)?.MinValue ?? 0;
        set
        {
            if (SelectedControl is not GaugeControl g) return;
            var old = g.MinValue;
            if (Math.Abs(old - value) < 0.001) return;
            _undoRedo.Execute(new SetPropertyAction("修改仪表最小值",
                () => g.MinValue = value, () => g.MinValue = old));
            OnPropertyChanged();
        }
    }

    /// <summary>仪表控件量程最大值代理属性。修改通过撤销/重做系统记录。</summary>
    public double GaugeMaxValue
    {
        get => (SelectedControl as GaugeControl)?.MaxValue ?? 100;
        set
        {
            if (SelectedControl is not GaugeControl g) return;
            var old = g.MaxValue;
            if (Math.Abs(old - value) < 0.001) return;
            _undoRedo.Execute(new SetPropertyAction("修改仪表最大值",
                () => g.MaxValue = value, () => g.MaxValue = old));
            OnPropertyChanged();
        }
    }

    /// <summary>仪表控件单位文本代理属性（例如 "℃"、"RPM"）。修改通过撤销/重做系统记录。</summary>
    public string GaugeUnit
    {
        get => (SelectedControl as GaugeControl)?.Unit ?? string.Empty;
        set
        {
            if (SelectedControl is not GaugeControl g) return;
            var old = g.Unit;
            if (old == value) return;
            _undoRedo.Execute(new SetPropertyAction("修改单位",
                () => g.Unit = value, () => g.Unit = old));
            OnPropertyChanged();
        }
    }

    // ── 指示灯专属属性 ───────────────────────────────────────────────────
    /// <summary>
    /// 指示灯当前亮灭状态的代理属性。修改通过撤销/重做系统记录。
    /// </summary>
    public bool LightIsOn
    {
        get => (SelectedControl as IndicatorLightControl)?.IsOn ?? false;
        set
        {
            if (SelectedControl is not IndicatorLightControl l) return;
            var old = l.IsOn;
            if (old == value) return;
            _undoRedo.Execute(new SetPropertyAction(value ? "点亮指示灯" : "关闭指示灯",
                () => l.IsOn = value, () => l.IsOn = old));
            OnPropertyChanged();
        }
    }

    /// <summary>指示灯点亮时的颜色代理属性。修改通过撤销/重做系统记录。</summary>
    public string LightOnColor
    {
        get => (SelectedControl as IndicatorLightControl)?.OnColor ?? string.Empty;
        set
        {
            if (SelectedControl is not IndicatorLightControl l) return;
            var old = l.OnColor;
            if (old == value) return;
            _undoRedo.Execute(new SetPropertyAction("修改亮色",
                () => l.OnColor = value, () => l.OnColor = old));
            OnPropertyChanged();
        }
    }

    /// <summary>指示灯熄灭时的颜色代理属性。修改通过撤销/重做系统记录。</summary>
    public string LightOffColor
    {
        get => (SelectedControl as IndicatorLightControl)?.OffColor ?? string.Empty;
        set
        {
            if (SelectedControl is not IndicatorLightControl l) return;
            var old = l.OffColor;
            if (old == value) return;
            _undoRedo.Execute(new SetPropertyAction("修改灭色",
                () => l.OffColor = value, () => l.OffColor = old));
            OnPropertyChanged();
        }
    }

    // ── 开关专属属性 ───────────────────────────────────────────────────
    /// <summary>
    /// 开关控件当前通断状态的代理属性。修改通过撤销/重做系统记录。
    /// </summary>
    public bool SwitchIsOn
    {
        get => (SelectedControl as SwitchControl)?.IsOn ?? false;
        set
        {
            if (SelectedControl is not SwitchControl s) return;
            var old = s.IsOn;
            if (old == value) return;
            _undoRedo.Execute(new SetPropertyAction(value ? "开关接通" : "开关断开",
                () => s.IsOn = value, () => s.IsOn = old));
            OnPropertyChanged();
        }
    }

    /// <summary>开关控件接通状态的显示标签文本代理属性。修改通过撤销/重做系统记录。</summary>
    public string SwitchOnLabel
    {
        get => (SelectedControl as SwitchControl)?.OnLabel ?? string.Empty;
        set
        {
            if (SelectedControl is not SwitchControl s) return;
            var old = s.OnLabel;
            if (old == value) return;
            _undoRedo.Execute(new SetPropertyAction("修改开标签",
                () => s.OnLabel = value, () => s.OnLabel = old));
            OnPropertyChanged();
        }
    }

    /// <summary>开关控件断开状态的显示标签文本代理属性。修改通过撤销/重做系统记录。</summary>
    public string SwitchOffLabel
    {
        get => (SelectedControl as SwitchControl)?.OffLabel ?? string.Empty;
        set
        {
            if (SelectedControl is not SwitchControl s) return;
            var old = s.OffLabel;
            if (old == value) return;
            _undoRedo.Execute(new SetPropertyAction("修改关标签",
                () => s.OffLabel = value, () => s.OffLabel = old));
            OnPropertyChanged();
        }
    }

    // ── 滑动条专属属性 ───────────────────────────────────────────────────
    /// <summary>
    /// 滑动条当前值的代理属性。修改通过撤销/重做系统记录。
    /// </summary>
    public double SliderValue
    {
        get => (SelectedControl as SliderControl)?.Value ?? 0;
        set
        {
            if (SelectedControl is not SliderControl s) return;
            var old = s.Value;
            if (Math.Abs(old - value) < 0.001) return;
            _undoRedo.Execute(new SetPropertyAction("修改滑动值",
                () => s.Value = value, () => s.Value = old));
            OnPropertyChanged();
        }
    }

    /// <summary>滑动条最小值代理属性。修改通过撤销/重做系统记录。</summary>
    public double SliderMinimum
    {
        get => (SelectedControl as SliderControl)?.Minimum ?? 0;
        set
        {
            if (SelectedControl is not SliderControl s) return;
            var old = s.Minimum;
            if (Math.Abs(old - value) < 0.001) return;
            _undoRedo.Execute(new SetPropertyAction("修改最小值",
                () => s.Minimum = value, () => s.Minimum = old));
            OnPropertyChanged();
        }
    }

    /// <summary>滑动条最大值代理属性。修改通过撤销/重做系统记录。</summary>
    public double SliderMaximum
    {
        get => (SelectedControl as SliderControl)?.Maximum ?? 100;
        set
        {
            if (SelectedControl is not SliderControl s) return;
            var old = s.Maximum;
            if (Math.Abs(old - value) < 0.001) return;
            _undoRedo.Execute(new SetPropertyAction("修改最大值",
                () => s.Maximum = value, () => s.Maximum = old));
            OnPropertyChanged();
        }
    }

    // ── 标签专属属性 ────────────────────────────────────────────────────
    /// <summary>
    /// 标签控件字体大小的代理属性（最小值为 1）。修改通过撤销/重做系统记录。
    /// </summary>
    public double LabelFontSize
    {
        get => (SelectedControl as LabelControl)?.FontSize ?? 14;
        set
        {
            if (SelectedControl is not LabelControl l || value < 1) return;
            var old = l.FontSize;
            if (Math.Abs(old - value) < 0.001) return;
            _undoRedo.Execute(new SetPropertyAction("修改字体大小",
                () => l.FontSize = value, () => l.FontSize = old));
            OnPropertyChanged();
        }
    }

    // ── 通用控件属性 ───────────────────────────────────────────────────
    /// <summary>
    /// 控件名称代理属性。修改通过撤销/重做系统记录，操作描述包含新名称。
    /// </summary>
    public string ControlName
    {
        get => SelectedControl?.Name ?? string.Empty;
        set
        {
            if (SelectedControl == null) return;
            var old = SelectedControl.Name;
            if (old == value) return;
            _undoRedo.Execute(new SetPropertyAction($"重命名 → {value}",
                () => SelectedControl.Name = value,
                () => SelectedControl.Name = old));
            OnPropertyChanged();
        }
    }

    /// <summary>控件 X 轴坐标（画布左上角为原点，向右为正方向）。修改通过撤销/重做系统记录。</summary>
    public double ControlX
    {
        get => SelectedControl?.X ?? 0;
        set
        {
            if (SelectedControl == null) return;
            var old = SelectedControl.X;
            if (Math.Abs(old - value) < 0.001) return;
            _undoRedo.Execute(new SetPropertyAction("修改 X",
                () => SelectedControl.X = value,
                () => SelectedControl.X = old));
            OnPropertyChanged();
        }
    }

    /// <summary>控件 Y 轴坐标（画布左上角为原点，向下为正方向）。修改通过撤销/重做系统记录。</summary>
    public double ControlY
    {
        get => SelectedControl?.Y ?? 0;
        set
        {
            if (SelectedControl == null) return;
            var old = SelectedControl.Y;
            if (Math.Abs(old - value) < 0.001) return;
            _undoRedo.Execute(new SetPropertyAction("修改 Y",
                () => SelectedControl.Y = value,
                () => SelectedControl.Y = old));
            OnPropertyChanged();
        }
    }

    /// <summary>控件宽度（像素，最小值为 1）。修改通过撤销/重做系统记录。</summary>
    public double ControlWidth
    {
        get => SelectedControl?.Width ?? 0;
        set
        {
            if (SelectedControl == null || value <= 0) return;
            var old = SelectedControl.Width;
            if (Math.Abs(old - value) < 0.001) return;
            _undoRedo.Execute(new SetPropertyAction("修改宽度",
                () => SelectedControl.Width = value,
                () => SelectedControl.Width = old));
            OnPropertyChanged();
        }
    }

    /// <summary>控件高度（像素，最小值为 1）。修改通过撤销/重做系统记录。</summary>
    public double ControlHeight
    {
        get => SelectedControl?.Height ?? 0;
        set
        {
            if (SelectedControl == null || value <= 0) return;
            var old = SelectedControl.Height;
            if (Math.Abs(old - value) < 0.001) return;
            _undoRedo.Execute(new SetPropertyAction("修改高度",
                () => SelectedControl.Height = value,
                () => SelectedControl.Height = old));
            OnPropertyChanged();
        }
    }

    /// <summary>控件是否可见（false 时控件在运行时隐藏）。修改通过撤销/重做系统记录。</summary>
    public bool ControlVisible
    {
        get => SelectedControl?.Visible ?? true;
        set
        {
            if (SelectedControl == null) return;
            var old = SelectedControl.Visible;
            if (old == value) return;
            _undoRedo.Execute(new SetPropertyAction(value ? "设为可见" : "设为隐藏",
                () => SelectedControl.Visible = value,
                () => SelectedControl.Visible = old));
            OnPropertyChanged();
        }
    }

    /// <summary>控件是否锁定（true 时不可通过画布拖拽移动或调整大小）。修改通过撤销/重做系统记录。</summary>
    public bool ControlLocked
    {
        get => SelectedControl?.Locked ?? false;
        set
        {
            if (SelectedControl == null) return;
            var old = SelectedControl.Locked;
            if (old == value) return;
            _undoRedo.Execute(new SetPropertyAction(value ? "锁定" : "解锁",
                () => SelectedControl.Locked = value,
                () => SelectedControl.Locked = old));
            OnPropertyChanged();
        }
    }

    /// <summary>控件透明度，范围 0.0（完全透明）～1.0（完全不透明）。修改通过撤销/重做系统记录。</summary>
    public double ControlOpacity
    {
        get => SelectedControl?.Opacity ?? 1.0;
        set
        {
            if (SelectedControl == null) return;
            var old = SelectedControl.Opacity;
            if (Math.Abs(old - value) < 0.001) return;
            _undoRedo.Execute(new SetPropertyAction("修改透明度",
                () => SelectedControl.Opacity = value,
                () => SelectedControl.Opacity = old));
            OnPropertyChanged();
        }
    }

    /// <summary>控件的 Z 轴层级索引，值越大显示在越上层。修改通过撤销/重做系统记录。</summary>
    public int ControlZIndex
    {
        get => SelectedControl?.ZIndex ?? 0;
        set
        {
            if (SelectedControl == null) return;
            var old = SelectedControl.ZIndex;
            if (old == value) return;
            _undoRedo.Execute(new SetPropertyAction("修改层级",
                () => SelectedControl.ZIndex = value,
                () => SelectedControl.ZIndex = old));
            OnPropertyChanged();
        }
    }
}
