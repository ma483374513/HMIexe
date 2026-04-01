namespace HMIexe.Core.Models.Controls;

public class LabelControl : HmiControlBase
{
    private string _text = "Label";
    private string _foregroundColor = "#000000";
    private double _fontSize = 14;
    private string _fontFamily = "Arial";
    private string _fontWeight = "Normal";
    private string _textAlignment = "Left";

    public override ControlType ControlType => ControlType.Label;

    public string Text
    {
        get => _text;
        set { _text = value; OnPropertyChanged(); }
    }

    public string ForegroundColor
    {
        get => _foregroundColor;
        set { _foregroundColor = value; OnPropertyChanged(); }
    }

    public double FontSize
    {
        get => _fontSize;
        set { _fontSize = value; OnPropertyChanged(); }
    }

    public string FontFamily
    {
        get => _fontFamily;
        set { _fontFamily = value; OnPropertyChanged(); }
    }

    public string FontWeight
    {
        get => _fontWeight;
        set { _fontWeight = value; OnPropertyChanged(); }
    }

    public string TextAlignment
    {
        get => _textAlignment;
        set { _textAlignment = value; OnPropertyChanged(); }
    }
}
