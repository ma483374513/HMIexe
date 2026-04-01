namespace HMIexe.Core.Models.Controls;

public class LineControl : HmiControlBase
{
    private string _strokeColor = "#FFFFFF";
    private double _strokeThickness = 2;
    private string _dashArray = string.Empty;
    private double _endX = 100;
    private double _endY;

    public override ControlType ControlType => ControlType.Line;

    public string StrokeColor { get => _strokeColor; set { _strokeColor = value; OnPropertyChanged(); } }
    public double StrokeThickness { get => _strokeThickness; set { _strokeThickness = value; OnPropertyChanged(); } }
    public string DashArray { get => _dashArray; set { _dashArray = value; OnPropertyChanged(); } }
    public double EndX { get => _endX; set { _endX = value; OnPropertyChanged(); } }
    public double EndY { get => _endY; set { _endY = value; OnPropertyChanged(); } }

    public LineControl()
    {
        Width = 100;
        Height = 4;
    }
}
