namespace HMIexe.Core.Models.Controls;

public class ButtonControl : HmiControlBase
{
    private string _text = "Button";
    private string _backgroundColor = "#4CAF50";
    private string _foregroundColor = "#FFFFFF";
    private string _iconPath = string.Empty;
    private string _targetPageId = string.Empty;
    private string _clickScript = string.Empty;

    public override ControlType ControlType => ControlType.Button;

    public string Text
    {
        get => _text;
        set { _text = value; OnPropertyChanged(); }
    }

    public string BackgroundColor
    {
        get => _backgroundColor;
        set { _backgroundColor = value; OnPropertyChanged(); }
    }

    public string ForegroundColor
    {
        get => _foregroundColor;
        set { _foregroundColor = value; OnPropertyChanged(); }
    }

    public string IconPath
    {
        get => _iconPath;
        set { _iconPath = value; OnPropertyChanged(); }
    }

    public string TargetPageId
    {
        get => _targetPageId;
        set { _targetPageId = value; OnPropertyChanged(); }
    }

    public string ClickScript
    {
        get => _clickScript;
        set { _clickScript = value; OnPropertyChanged(); }
    }
}
