/// <summary>
/// 圆形控件模型。
/// 继承自 HmiControlBase，表示一个正圆形图形控件，支持填充颜色、描边颜色和描边厚度配置。
/// 默认宽高均为 80px 以保证正圆比例。
/// </summary>
namespace HMIexe.Core.Models.Controls;

/// <summary>
/// HMI 圆形控件，用于在画布上绘制正圆形图形。
/// </summary>
public class CircleControl : HmiControlBase
{
    // 私有字段：填充颜色，默认透明
    private string _fillColor = "Transparent";
    // 私有字段：描边颜色，默认白色
    private string _strokeColor = "#FFFFFF";
    // 私有字段：描边线条粗细，默认 2px
    private double _strokeThickness = 2;

    /// <summary>控件类型标识，固定返回 ControlType.Circle。</summary>
    public override ControlType ControlType => ControlType.Circle;

    /// <summary>圆形填充颜色，支持颜色名称或十六进制字符串，默认为透明。</summary>
    public string FillColor { get => _fillColor; set { _fillColor = value; OnPropertyChanged(); } }

    /// <summary>圆形边框描边颜色，默认为白色 "#FFFFFF"。</summary>
    public string StrokeColor { get => _strokeColor; set { _strokeColor = value; OnPropertyChanged(); } }

    /// <summary>描边线条粗细（像素），默认为 2。</summary>
    public double StrokeThickness { get => _strokeThickness; set { _strokeThickness = value; OnPropertyChanged(); } }

    /// <summary>初始化圆形控件，默认宽高均为 80px 以保持正圆比例。</summary>
    public CircleControl()
    {
        Width = 80;
        Height = 80;
    }
}
