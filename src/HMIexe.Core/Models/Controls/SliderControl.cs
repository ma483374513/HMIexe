namespace HMIexe.Core.Models.Controls;

public class SliderControl : HmiControlBase
{
    private double _value;
    private double _minimum;
    private double _maximum = 100;
    private double _tickFrequency = 10;
    private bool _isSnapToTick;
    private bool _isVertical;
    private string _unit = string.Empty;
    private bool _showValue = true;
    private string _valueChangedScript = string.Empty;

    public override ControlType ControlType => ControlType.Slider;

    public double Value { get => _value; set { _value = value; OnPropertyChanged(); } }
    public double Minimum { get => _minimum; set { _minimum = value; OnPropertyChanged(); } }
    public double Maximum { get => _maximum; set { _maximum = value; OnPropertyChanged(); } }
    public double TickFrequency { get => _tickFrequency; set { _tickFrequency = value; OnPropertyChanged(); } }
    public bool IsSnapToTick { get => _isSnapToTick; set { _isSnapToTick = value; OnPropertyChanged(); } }
    public bool IsVertical { get => _isVertical; set { _isVertical = value; OnPropertyChanged(); } }
    public string Unit { get => _unit; set { _unit = value; OnPropertyChanged(); } }
    public bool ShowValue { get => _showValue; set { _showValue = value; OnPropertyChanged(); } }
    public string ValueChangedScript { get => _valueChangedScript; set { _valueChangedScript = value; OnPropertyChanged(); } }

    public SliderControl()
    {
        Width = 200;
        Height = 40;
    }
}
