namespace HMIexe.Core.Models.Controls;

public class SwitchControl : HmiControlBase
{
    private bool _isOn;
    private string _onLabel = "ON";
    private string _offLabel = "OFF";
    private string _onColor = "#4CAF50";
    private string _offColor = "#9E9E9E";
    private string _toggleScript = string.Empty;

    public override ControlType ControlType => ControlType.Switch;

    public bool IsOn { get => _isOn; set { _isOn = value; OnPropertyChanged(); } }
    public string OnLabel { get => _onLabel; set { _onLabel = value; OnPropertyChanged(); } }
    public string OffLabel { get => _offLabel; set { _offLabel = value; OnPropertyChanged(); } }
    public string OnColor { get => _onColor; set { _onColor = value; OnPropertyChanged(); } }
    public string OffColor { get => _offColor; set { _offColor = value; OnPropertyChanged(); } }
    public string ToggleScript { get => _toggleScript; set { _toggleScript = value; OnPropertyChanged(); } }

    public SwitchControl()
    {
        Width = 80;
        Height = 36;
    }
}
