/// <summary>
/// 标签控件模型。
/// 继承自 HmiControlBase，用于在画布上显示静态或数据绑定的文本内容，
/// 支持字体族、字号、字重、对齐方式和前景色配置。
/// </summary>
namespace HMIexe.Core.Models.Controls;

/// <summary>
/// HMI 标签控件，用于展示文本信息，如设备名称、数值单位、状态说明等。
/// </summary>
public class LabelControl : HmiControlBase
{
    // 私有字段：显示文本内容
    private string _text = "Label";
    // 私有字段：文字颜色，默认黑色
    private string _foregroundColor = "#000000";
    // 私有字段：字体大小，默认 14pt
    private double _fontSize = 14;
    // 私有字段：字体族名称，默认 Arial
    private string _fontFamily = "Arial";
    // 私有字段：字体粗细，默认正常
    private string _fontWeight = "Normal";
    // 私有字段：文本对齐方式，默认左对齐
    private string _textAlignment = "Left";

    /// <summary>控件类型标识，固定返回 ControlType.Label。</summary>
    public override ControlType ControlType => ControlType.Label;

    /// <summary>标签显示的文本内容，可通过数据绑定动态更新。</summary>
    public string Text
    {
        get => _text;
        set { _text = value; OnPropertyChanged(); }
    }

    /// <summary>文字颜色，十六进制颜色字符串，默认为黑色 "#000000"。</summary>
    public string ForegroundColor
    {
        get => _foregroundColor;
        set { _foregroundColor = value; OnPropertyChanged(); }
    }

    /// <summary>字体大小（磅），默认为 14。</summary>
    public double FontSize
    {
        get => _fontSize;
        set { _fontSize = value; OnPropertyChanged(); }
    }

    /// <summary>字体族名称（如 "Arial"、"微软雅黑"），默认为 "Arial"。</summary>
    public string FontFamily
    {
        get => _fontFamily;
        set { _fontFamily = value; OnPropertyChanged(); }
    }

    /// <summary>字体粗细（如 "Normal"、"Bold"），默认为 "Normal"。</summary>
    public string FontWeight
    {
        get => _fontWeight;
        set { _fontWeight = value; OnPropertyChanged(); }
    }

    /// <summary>文本水平对齐方式（"Left"、"Center"、"Right"），默认为左对齐 "Left"。</summary>
    public string TextAlignment
    {
        get => _textAlignment;
        set { _textAlignment = value; OnPropertyChanged(); }
    }
}
