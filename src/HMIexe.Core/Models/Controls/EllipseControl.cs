/// <summary>
/// 椭圆控件模型。
/// 继承自 HmiControlBase，表示一个椭圆形图形控件，支持填充颜色、描边颜色和描边厚度配置。
/// 默认宽 120px、高 80px 以体现椭圆形状。
/// </summary>
namespace HMIexe.Core.Models.Controls;

/// <summary>
/// HMI 椭圆控件，用于在画布上绘制椭圆形图形。
/// </summary>
public class EllipseControl : HmiControlBase
{
    // 私有字段：填充颜色，默认透明
    private string _fillColor = "Transparent";
    // 私有字段：描边颜色，默认白色
    private string _strokeColor = "#FFFFFF";
    // 私有字段：描边线条粗细，默认 2px
    private double _strokeThickness = 2;

    /// <summary>控件类型标识，固定返回 ControlType.Ellipse。</summary>
    public override ControlType ControlType => ControlType.Ellipse;

    /// <summary>椭圆填充颜色，支持颜色名称或十六进制字符串，默认为透明。</summary>
    public string FillColor { get => _fillColor; set { _fillColor = value; OnPropertyChanged(); } }

    /// <summary>椭圆边框描边颜色，默认为白色 "#FFFFFF"。</summary>
    public string StrokeColor { get => _strokeColor; set { _strokeColor = value; OnPropertyChanged(); } }

    /// <summary>描边线条粗细（像素），默认为 2。</summary>
    public double StrokeThickness { get => _strokeThickness; set { _strokeThickness = value; OnPropertyChanged(); } }

    /// <summary>初始化椭圆控件，默认宽 120px、高 80px。</summary>
    public EllipseControl()
    {
        Width = 120;
        Height = 80;
    }
}
