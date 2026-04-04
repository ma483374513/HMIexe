/// <summary>
/// 仪表盘控件模型。
/// 继承自 HmiControlBase，以圆形仪表盘样式可视化模拟量数值，
/// 支持量程范围、工程单位、刻度数量及指针样式配置。
/// </summary>
namespace HMIexe.Core.Models.Controls;

/// <summary>
/// HMI 仪表盘控件，以指针表盘的形式显示数值，常用于显示温度、压力、转速等模拟量。
/// </summary>
public class GaugeControl : HmiControlBase
{
    // 私有字段：当前显示数值
    private double _value;
    // 私有字段：量程最小值
    private double _minValue;
    // 私有字段：量程最大值，默认 100
    private double _maxValue = 100;
    // 私有字段：工程单位字符串
    private string _unit = string.Empty;
    // 私有字段：刻度线数量，默认 10
    private int _tickCount = 10;
    // 私有字段：指针样式标识
    private string _pointerStyle = "Default";

    /// <summary>控件类型标识，固定返回 ControlType.Gauge。</summary>
    public override ControlType ControlType => ControlType.Gauge;

    /// <summary>仪表盘当前显示的数值，应在 MinValue 和 MaxValue 之间。</summary>
    public double Value
    {
        get => _value;
        set { _value = value; OnPropertyChanged(); }
    }

    /// <summary>仪表盘量程的最小值，对应指针最左/下端位置。</summary>
    public double MinValue
    {
        get => _minValue;
        set { _minValue = value; OnPropertyChanged(); }
    }

    /// <summary>仪表盘量程的最大值，对应指针最右/上端位置，默认为 100。</summary>
    public double MaxValue
    {
        get => _maxValue;
        set { _maxValue = value; OnPropertyChanged(); }
    }

    /// <summary>显示在表盘上的工程单位（如 "℃"、"rpm"、"MPa"），为空时不显示单位。</summary>
    public string Unit
    {
        get => _unit;
        set { _unit = value; OnPropertyChanged(); }
    }

    /// <summary>表盘刻度线的数量，决定刻度盘的精细程度，默认为 10。</summary>
    public int TickCount
    {
        get => _tickCount;
        set { _tickCount = value; OnPropertyChanged(); }
    }

    /// <summary>指针外观样式标识符，渲染层根据此值选择对应的指针模板，默认为 "Default"。</summary>
    public string PointerStyle
    {
        get => _pointerStyle;
        set { _pointerStyle = value; OnPropertyChanged(); }
    }
}
