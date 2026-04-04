/// <summary>
/// 矩形控件模型。
/// 继承自 HmiControlBase，在画布上绘制矩形或圆角矩形，
/// 支持填充颜色、描边颜色、描边粗细及圆角半径配置。
/// 默认宽 120px、高 80px。
/// </summary>
namespace HMIexe.Core.Models.Controls;

/// <summary>
/// HMI 矩形控件，用于绘制矩形区域，可作为背景面板、分组边框或装饰元素。
/// </summary>
public class RectangleControl : HmiControlBase
{
    // 私有字段：填充颜色，默认透明
    private string _fillColor = "Transparent";
    // 私有字段：描边颜色，默认白色
    private string _strokeColor = "#FFFFFF";
    // 私有字段：描边粗细，默认 2px
    private double _strokeThickness = 2;
    // 私有字段：圆角半径，默认 0（直角）
    private double _cornerRadius;

    /// <summary>控件类型标识，固定返回 ControlType.Rectangle。</summary>
    public override ControlType ControlType => ControlType.Rectangle;

    /// <summary>矩形填充颜色，支持颜色名称或十六进制字符串，默认为透明。</summary>
    public string FillColor { get => _fillColor; set { _fillColor = value; OnPropertyChanged(); } }

    /// <summary>矩形边框描边颜色，默认为白色 "#FFFFFF"。</summary>
    public string StrokeColor { get => _strokeColor; set { _strokeColor = value; OnPropertyChanged(); } }

    /// <summary>描边线条粗细（像素），默认为 2。</summary>
    public double StrokeThickness { get => _strokeThickness; set { _strokeThickness = value; OnPropertyChanged(); } }

    /// <summary>矩形圆角半径（像素），为 0 时为直角矩形，值越大圆角越明显。</summary>
    public double CornerRadius { get => _cornerRadius; set { _cornerRadius = value; OnPropertyChanged(); } }

    /// <summary>初始化矩形控件，默认宽 120px、高 80px。</summary>
    public RectangleControl()
    {
        Width = 120;
        Height = 80;
    }
}
