using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using HMIexe.Core.Models.Canvas;
using HMIexe.Core.Models.Controls;
using HMIexe.Core.Services;
using HMIexe.Core.UndoRedo;

namespace HMIexe.App.ViewModels;

public partial class PropertyPanelViewModel : ObservableObject
{
    private readonly UndoRedoHistory _undoRedo;
    private readonly IVariableService? _variableService;

    [ObservableProperty]
    private HmiControlBase? _selectedControl;

    [ObservableProperty]
    private HmiPage? _currentPage;

    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>All variable names available for binding (populated from IVariableService).</summary>
    public ObservableCollection<string> AvailableVariables { get; } = new() { string.Empty };

    public PropertyPanelViewModel(UndoRedoHistory undoRedo, IVariableService? variableService = null)
    {
        _undoRedo = undoRedo;
        _variableService = variableService;
        if (_variableService != null)
        {
            RefreshVariableList();
            // Keep list in sync as variables are added/removed
            _variableService.VariableValueChanged += (_, _) => { /* Value changes don't affect names */ };
        }
    }

    public void RefreshVariableList()
    {
        AvailableVariables.Clear();
        AvailableVariables.Add(string.Empty);
        if (_variableService == null) return;
        foreach (var v in _variableService.Variables)
            AvailableVariables.Add(v.Name);
    }

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

    public bool HasSelectedControl => SelectedControl != null;

    /// <summary>True for controls that have an editable Text/Label property.</summary>
    public bool HasControlText => SelectedControl is ButtonControl
        or LabelControl or TextBoxControl;

    // ── Type guards ──────────────────────────────────────────────────
    public bool IsButtonControl => SelectedControl is ButtonControl;
    public bool IsLabelControl => SelectedControl is LabelControl;
    public bool IsGaugeControl => SelectedControl is GaugeControl;
    public bool IsIndicatorLightControl => SelectedControl is IndicatorLightControl;
    public bool IsSwitchControl => SelectedControl is SwitchControl;
    public bool IsSliderControl => SelectedControl is SliderControl;
    public bool IsShapeControl => SelectedControl is RectangleControl
        or CircleControl or EllipseControl or LineControl;

    // ── Variable binding ─────────────────────────────────────────────
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

    // ── Text ─────────────────────────────────────────────────────────
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

    // ── Colors ───────────────────────────────────────────────────────
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

    // ── Gauge ────────────────────────────────────────────────────────
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

    // ── IndicatorLight ───────────────────────────────────────────────
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

    // ── Switch ───────────────────────────────────────────────────────
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

    // ── Slider ───────────────────────────────────────────────────────
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

    // ── Label ────────────────────────────────────────────────────────
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

    // ── Common (unchanged) ───────────────────────────────────────────
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
