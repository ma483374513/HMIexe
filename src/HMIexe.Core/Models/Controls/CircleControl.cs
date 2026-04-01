namespace HMIexe.Core.Models.Controls;

public class CircleControl : HmiControlBase
{
    private string _fillColor = "Transparent";
    private string _strokeColor = "#FFFFFF";
    private double _strokeThickness = 2;

    public override ControlType ControlType => ControlType.Circle;

    public string FillColor { get => _fillColor; set { _fillColor = value; OnPropertyChanged(); } }
    public string StrokeColor { get => _strokeColor; set { _strokeColor = value; OnPropertyChanged(); } }
    public double StrokeThickness { get => _strokeThickness; set { _strokeThickness = value; OnPropertyChanged(); } }

    public CircleControl()
    {
        Width = 80;
        Height = 80;
    }
}
