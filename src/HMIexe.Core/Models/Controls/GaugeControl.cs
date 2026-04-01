namespace HMIexe.Core.Models.Controls;

public class GaugeControl : HmiControlBase
{
    private double _value;
    private double _minValue;
    private double _maxValue = 100;
    private string _unit = string.Empty;
    private int _tickCount = 10;
    private string _pointerStyle = "Default";

    public override ControlType ControlType => ControlType.Gauge;

    public double Value
    {
        get => _value;
        set { _value = value; OnPropertyChanged(); }
    }

    public double MinValue
    {
        get => _minValue;
        set { _minValue = value; OnPropertyChanged(); }
    }

    public double MaxValue
    {
        get => _maxValue;
        set { _maxValue = value; OnPropertyChanged(); }
    }

    public string Unit
    {
        get => _unit;
        set { _unit = value; OnPropertyChanged(); }
    }

    public int TickCount
    {
        get => _tickCount;
        set { _tickCount = value; OnPropertyChanged(); }
    }

    public string PointerStyle
    {
        get => _pointerStyle;
        set { _pointerStyle = value; OnPropertyChanged(); }
    }
}
