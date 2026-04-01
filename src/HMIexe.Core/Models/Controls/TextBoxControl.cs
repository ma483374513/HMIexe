namespace HMIexe.Core.Models.Controls;

public enum TextBoxMode { Text, Password, Number }

public class TextBoxControl : HmiControlBase
{
    private string _text = string.Empty;
    private string _placeholder = string.Empty;
    private TextBoxMode _mode = TextBoxMode.Text;
    private double? _minValue, _maxValue;
    private string _validationPattern = string.Empty;

    public override ControlType ControlType => ControlType.TextBox;

    public string Text
    {
        get => _text;
        set { _text = value; OnPropertyChanged(); }
    }

    public string Placeholder
    {
        get => _placeholder;
        set { _placeholder = value; OnPropertyChanged(); }
    }

    public TextBoxMode Mode
    {
        get => _mode;
        set { _mode = value; OnPropertyChanged(); }
    }

    public double? MinValue
    {
        get => _minValue;
        set { _minValue = value; OnPropertyChanged(); }
    }

    public double? MaxValue
    {
        get => _maxValue;
        set { _maxValue = value; OnPropertyChanged(); }
    }

    public string ValidationPattern
    {
        get => _validationPattern;
        set { _validationPattern = value; OnPropertyChanged(); }
    }
}
