namespace HMIexe.Core.Models.Controls;

public class RectangleControl : HmiControlBase
{
    private string _fillColor = "Transparent";
    private string _strokeColor = "#FFFFFF";
    private double _strokeThickness = 2;
    private double _cornerRadius;

    public override ControlType ControlType => ControlType.Rectangle;

    public string FillColor { get => _fillColor; set { _fillColor = value; OnPropertyChanged(); } }
    public string StrokeColor { get => _strokeColor; set { _strokeColor = value; OnPropertyChanged(); } }
    public double StrokeThickness { get => _strokeThickness; set { _strokeThickness = value; OnPropertyChanged(); } }
    public double CornerRadius { get => _cornerRadius; set { _cornerRadius = value; OnPropertyChanged(); } }

    public RectangleControl()
    {
        Width = 120;
        Height = 80;
    }
}
