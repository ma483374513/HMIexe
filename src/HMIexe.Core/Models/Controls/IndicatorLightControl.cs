namespace HMIexe.Core.Models.Controls;

public class IndicatorLightControl : HmiControlBase
{
    private bool _isOn;
    private string _onColor = "#00FF00";
    private string _offColor = "#444444";
    private string _borderColor = "#222222";
    private double _borderThickness = 2;
    private bool _blinkWhenOn;
    private int _blinkIntervalMs = 500;

    public override ControlType ControlType => ControlType.IndicatorLight;

    public bool IsOn { get => _isOn; set { _isOn = value; OnPropertyChanged(); } }
    public string OnColor { get => _onColor; set { _onColor = value; OnPropertyChanged(); } }
    public string OffColor { get => _offColor; set { _offColor = value; OnPropertyChanged(); } }
    public string BorderColor { get => _borderColor; set { _borderColor = value; OnPropertyChanged(); } }
    public double BorderThickness { get => _borderThickness; set { _borderThickness = value; OnPropertyChanged(); } }
    public bool BlinkWhenOn { get => _blinkWhenOn; set { _blinkWhenOn = value; OnPropertyChanged(); } }
    public int BlinkIntervalMs { get => _blinkIntervalMs; set { _blinkIntervalMs = value; OnPropertyChanged(); } }

    public IndicatorLightControl()
    {
        Width = 40;
        Height = 40;
    }
}
