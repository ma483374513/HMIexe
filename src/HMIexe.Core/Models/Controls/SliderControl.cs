/// <summary>
/// 滑块控件模型。
/// 继承自 HmiControlBase，通过拖动滑块设置连续数值，支持量程范围、刻度吸附、
/// 方向（水平/垂直）、单位显示及数值变更脚本配置。
/// 默认宽 200px、高 40px。
/// </summary>
namespace HMIexe.Core.Models.Controls;

/// <summary>
/// HMI 滑块控件，用于通过拖动方式输入或调节模拟量数值（如设定值、亮度、速度等）。
/// </summary>
public class SliderControl : HmiControlBase
{
    // 私有字段：当前值
    private double _value;
    // 私有字段：最小值
    private double _minimum;
    // 私有字段：最大值，默认 100
    private double _maximum = 100;
    // 私有字段：刻度间隔，默认 10
    private double _tickFrequency = 10;
    // 私有字段：是否吸附到刻度位置
    private bool _isSnapToTick;
    // 私有字段：是否为垂直方向
    private bool _isVertical;
    // 私有字段：工程单位
    private string _unit = string.Empty;
    // 私有字段：是否在滑块旁显示当前数值，默认显示
    private bool _showValue = true;
    // 私有字段：数值变化时执行的脚本代码
    private string _valueChangedScript = string.Empty;

    /// <summary>控件类型标识，固定返回 ControlType.Slider。</summary>
    public override ControlType ControlType => ControlType.Slider;

    /// <summary>滑块当前值，应在 Minimum 和 Maximum 之间。</summary>
    public double Value { get => _value; set { _value = value; OnPropertyChanged(); } }

    /// <summary>滑块量程最小值，默认为 0。</summary>
    public double Minimum { get => _minimum; set { _minimum = value; OnPropertyChanged(); } }

    /// <summary>滑块量程最大值，默认为 100。</summary>
    public double Maximum { get => _maximum; set { _maximum = value; OnPropertyChanged(); } }

    /// <summary>刻度线间隔，决定吸附精度和刻度标记密度，默认为 10。</summary>
    public double TickFrequency { get => _tickFrequency; set { _tickFrequency = value; OnPropertyChanged(); } }

    /// <summary>是否将滑块位置吸附到最近的刻度点；启用后输入值为离散刻度值。</summary>
    public bool IsSnapToTick { get => _isSnapToTick; set { _isSnapToTick = value; OnPropertyChanged(); } }

    /// <summary>是否为垂直方向滑块；为 false 时为水平方向（默认）。</summary>
    public bool IsVertical { get => _isVertical; set { _isVertical = value; OnPropertyChanged(); } }

    /// <summary>显示在滑块旁的工程单位（如 "℃"、"%"），为空时不显示。</summary>
    public string Unit { get => _unit; set { _unit = value; OnPropertyChanged(); } }

    /// <summary>是否在滑块旁实时显示当前数值，默认为 true。</summary>
    public bool ShowValue { get => _showValue; set { _showValue = value; OnPropertyChanged(); } }

    /// <summary>滑块值改变时执行的脚本代码，可用于实时写入变量或执行联动逻辑。</summary>
    public string ValueChangedScript { get => _valueChangedScript; set { _valueChangedScript = value; OnPropertyChanged(); } }

    /// <summary>初始化滑块控件，默认宽 200px、高 40px。</summary>
    public SliderControl()
    {
        Width = 200;
        Height = 40;
    }
}
