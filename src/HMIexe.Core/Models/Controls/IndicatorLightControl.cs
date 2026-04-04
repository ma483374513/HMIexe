/// <summary>
/// 指示灯控件模型。
/// 继承自 HmiControlBase，通过颜色变化（亮/灭）反映开关量的状态。
/// 支持亮灭颜色自定义、边框样式以及闪烁功能配置。
/// 默认宽高均为 40px。
/// </summary>
namespace HMIexe.Core.Models.Controls;

/// <summary>
/// HMI 指示灯控件，用于直观显示设备或信号的开关状态。
/// 当 <see cref="IsOn"/> 为 true 时显示 <see cref="OnColor"/>，否则显示 <see cref="OffColor"/>。
/// </summary>
public class IndicatorLightControl : HmiControlBase
{
    // 私有字段：当前开关状态，默认熄灭
    private bool _isOn;
    // 私有字段：亮灯颜色，默认绿色
    private string _onColor = "#00FF00";
    // 私有字段：灭灯颜色，默认深灰色
    private string _offColor = "#444444";
    // 私有字段：边框颜色，默认深黑色
    private string _borderColor = "#222222";
    // 私有字段：边框粗细，默认 2px
    private double _borderThickness = 2;
    // 私有字段：亮灯时是否闪烁
    private bool _blinkWhenOn;
    // 私有字段：闪烁间隔（毫秒），默认 500ms
    private int _blinkIntervalMs = 500;

    /// <summary>控件类型标识，固定返回 ControlType.IndicatorLight。</summary>
    public override ControlType ControlType => ControlType.IndicatorLight;

    /// <summary>
    /// 指示灯当前的开关状态。
    /// 设置此属性时同时通知 <see cref="CurrentColor"/> 属性变更，确保 UI 即时刷新颜色。
    /// </summary>
    public bool IsOn
    {
        get => _isOn;
        set
        {
            _isOn = value;
            OnPropertyChanged();
            // 同步通知 CurrentColor 派生属性，触发 UI 颜色刷新
            OnPropertyChanged(nameof(CurrentColor));
        }
    }

    /// <summary>
    /// 指示灯亮灯时的颜色，默认为绿色 "#00FF00"。
    /// 设置后同时通知 <see cref="CurrentColor"/> 刷新。
    /// </summary>
    public string OnColor
    {
        get => _onColor;
        set
        {
            _onColor = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentColor));
        }
    }

    /// <summary>
    /// 指示灯熄灭时的颜色，默认为深灰色 "#444444"。
    /// 设置后同时通知 <see cref="CurrentColor"/> 刷新。
    /// </summary>
    public string OffColor
    {
        get => _offColor;
        set
        {
            _offColor = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentColor));
        }
    }

    /// <summary>指示灯边框颜色，默认为深黑色 "#222222"。</summary>
    public string BorderColor { get => _borderColor; set { _borderColor = value; OnPropertyChanged(); } }

    /// <summary>指示灯边框粗细（像素），默认为 2。</summary>
    public double BorderThickness { get => _borderThickness; set { _borderThickness = value; OnPropertyChanged(); } }

    /// <summary>是否在亮灯时启用闪烁效果；为 false 时常亮不闪烁。</summary>
    public bool BlinkWhenOn { get => _blinkWhenOn; set { _blinkWhenOn = value; OnPropertyChanged(); } }

    /// <summary>闪烁间隔时间（毫秒），即亮灭交替的周期，默认为 500ms。</summary>
    public int BlinkIntervalMs { get => _blinkIntervalMs; set { _blinkIntervalMs = value; OnPropertyChanged(); } }

    /// <summary>Current display color: OnColor when IsOn, otherwise OffColor.</summary>
    public string CurrentColor => IsOn ? OnColor : OffColor;

    /// <summary>初始化指示灯控件，默认宽高均为 40px。</summary>
    public IndicatorLightControl()
    {
        Width = 40;
        Height = 40;
    }
}
