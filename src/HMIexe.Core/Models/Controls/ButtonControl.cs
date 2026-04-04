/// <summary>
/// 按钮控件模型。
/// 继承自 HmiControlBase，支持文本标签、背景/前景颜色、图标路径、
/// 页面跳转目标及点击事件脚本等属性配置。
/// </summary>
namespace HMIexe.Core.Models.Controls;

/// <summary>
/// HMI 按钮控件。
/// 用于触发操作，可配置点击脚本或直接跳转到指定页面。
/// </summary>
public class ButtonControl : HmiControlBase
{
    // 私有字段：按钮显示文本
    private string _text = "Button";
    // 私有字段：按钮背景颜色，默认绿色
    private string _backgroundColor = "#4CAF50";
    // 私有字段：文字前景颜色，默认白色
    private string _foregroundColor = "#FFFFFF";
    // 私有字段：可选图标路径
    private string _iconPath = string.Empty;
    // 私有字段：点击后跳转的目标页面 ID
    private string _targetPageId = string.Empty;
    // 私有字段：点击时执行的脚本代码
    private string _clickScript = string.Empty;

    /// <summary>控件类型标识，固定返回 ControlType.Button。</summary>
    public override ControlType ControlType => ControlType.Button;

    /// <summary>按钮上显示的文本内容。</summary>
    public string Text
    {
        get => _text;
        set { _text = value; OnPropertyChanged(); }
    }

    /// <summary>按钮背景颜色，十六进制颜色字符串，默认为 "#4CAF50"（绿色）。</summary>
    public string BackgroundColor
    {
        get => _backgroundColor;
        set { _backgroundColor = value; OnPropertyChanged(); }
    }

    /// <summary>按钮文字颜色，十六进制颜色字符串，默认为 "#FFFFFF"（白色）。</summary>
    public string ForegroundColor
    {
        get => _foregroundColor;
        set { _foregroundColor = value; OnPropertyChanged(); }
    }

    /// <summary>按钮图标的资源路径；为空字符串时不显示图标。</summary>
    public string IconPath
    {
        get => _iconPath;
        set { _iconPath = value; OnPropertyChanged(); }
    }

    /// <summary>点击按钮后跳转的目标页面 ID；为空时不执行页面跳转。</summary>
    public string TargetPageId
    {
        get => _targetPageId;
        set { _targetPageId = value; OnPropertyChanged(); }
    }

    /// <summary>点击按钮时执行的脚本代码；为空时不执行脚本。</summary>
    public string ClickScript
    {
        get => _clickScript;
        set { _clickScript = value; OnPropertyChanged(); }
    }
}
