/// <summary>
/// 直线控件模型。
/// 继承自 HmiControlBase，在画布上绘制一条直线段，支持描边颜色、粗细、虚线样式及终点坐标配置。
/// 默认宽 100px、高 4px。
/// </summary>
namespace HMIexe.Core.Models.Controls;

/// <summary>
/// HMI 直线控件，用于在画布上绘制分隔线、连接线或示意线段。
/// 起点由基类的 X/Y 坐标决定，终点由 <see cref="EndX"/>/<see cref="EndY"/> 相对偏移指定。
/// </summary>
public class LineControl : HmiControlBase
{
    // 私有字段：描边颜色，默认白色
    private string _strokeColor = "#FFFFFF";
    // 私有字段：线条粗细，默认 2px
    private double _strokeThickness = 2;
    // 私有字段：虚线样式，空字符串表示实线
    private string _dashArray = string.Empty;
    // 私有字段：终点 X 坐标（相对于起点的偏移），默认 100px
    private double _endX = 100;
    // 私有字段：终点 Y 坐标（相对于起点的偏移），默认 0（水平线）
    private double _endY;

    /// <summary>控件类型标识，固定返回 ControlType.Line。</summary>
    public override ControlType ControlType => ControlType.Line;

    /// <summary>线条描边颜色，默认为白色 "#FFFFFF"。</summary>
    public string StrokeColor { get => _strokeColor; set { _strokeColor = value; OnPropertyChanged(); } }

    /// <summary>线条粗细（像素），默认为 2。</summary>
    public double StrokeThickness { get => _strokeThickness; set { _strokeThickness = value; OnPropertyChanged(); } }

    /// <summary>虚线样式字符串（如 "4 2" 表示 4px 实线 + 2px 间隔）；为空时为实线。</summary>
    public string DashArray { get => _dashArray; set { _dashArray = value; OnPropertyChanged(); } }

    /// <summary>线段终点的 X 坐标（相对于线段起点的偏移量），默认为 100。</summary>
    public double EndX { get => _endX; set { _endX = value; OnPropertyChanged(); } }

    /// <summary>线段终点的 Y 坐标（相对于线段起点的偏移量），默认为 0（水平方向）。</summary>
    public double EndY { get => _endY; set { _endY = value; OnPropertyChanged(); } }

    /// <summary>初始化直线控件，默认宽 100px、高 4px（代表线条的可交互边界框）。</summary>
    public LineControl()
    {
        Width = 100;
        Height = 4;
    }
}
