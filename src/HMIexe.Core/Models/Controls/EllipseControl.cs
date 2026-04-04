namespace HMIexe.Core.Models.Controls;

public class EllipseControl : HmiControlBase
{
    private string _fillColor = "Transparent";
    private string _strokeColor = "#FFFFFF";
    private double _strokeThickness = 2;

    public override ControlType ControlType => ControlType.Ellipse;

    public string FillColor { get => _fillColor; set { _fillColor = value; OnPropertyChanged(); } }
    public string StrokeColor { get => _strokeColor; set { _strokeColor = value; OnPropertyChanged(); } }
    public double StrokeThickness { get => _strokeThickness; set { _strokeThickness = value; OnPropertyChanged(); } }

    public EllipseControl()
    {
        Width = 120;
        Height = 80;
    }
}
