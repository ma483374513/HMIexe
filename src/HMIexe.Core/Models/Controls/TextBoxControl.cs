/// <summary>
/// 文本输入框控件模型。
/// 继承自 HmiControlBase，提供文本、密码和数字三种输入模式，
/// 支持占位提示文本、数值范围校验和正则验证图案配置。
/// </summary>
namespace HMIexe.Core.Models.Controls;

/// <summary>
/// 文本输入框的输入模式枚举。
/// </summary>
public enum TextBoxMode
{
    /// <summary>普通文本输入模式，显示明文。</summary>
    Text,
    /// <summary>密码输入模式，输入内容以掩码字符显示。</summary>
    Password,
    /// <summary>数字输入模式，限制只能输入数值，可配合 MinValue/MaxValue 进行范围校验。</summary>
    Number
}

/// <summary>
/// HMI 文本输入框控件，用于接收操作员的文本或数值输入。
/// 支持输入验证（数值范围和正则表达式），可通过数据绑定将输入值写回 HMI 变量。
/// </summary>
public class TextBoxControl : HmiControlBase
{
    // 私有字段：当前文本内容
    private string _text = string.Empty;
    // 私有字段：输入框为空时显示的占位提示文字
    private string _placeholder = string.Empty;
    // 私有字段：输入模式，默认为普通文本
    private TextBoxMode _mode = TextBoxMode.Text;
    // 私有字段：数字模式下的最小允许值
    private double? _minValue, _maxValue;
    // 私有字段：正则表达式验证图案
    private string _validationPattern = string.Empty;

    /// <summary>控件类型标识，固定返回 ControlType.TextBox。</summary>
    public override ControlType ControlType => ControlType.TextBox;

    /// <summary>输入框中的当前文本内容，通过数据绑定可读写对应 HMI 变量。</summary>
    public string Text
    {
        get => _text;
        set { _text = value; OnPropertyChanged(); }
    }

    /// <summary>输入框为空时显示的占位提示文字（灰色），为空字符串时不显示提示。</summary>
    public string Placeholder
    {
        get => _placeholder;
        set { _placeholder = value; OnPropertyChanged(); }
    }

    /// <summary>输入模式（文本/密码/数字），决定输入框的交互行为和显示方式。</summary>
    public TextBoxMode Mode
    {
        get => _mode;
        set { _mode = value; OnPropertyChanged(); }
    }

    /// <summary>数字输入模式下允许的最小值；为 null 时不限制下限。</summary>
    public double? MinValue
    {
        get => _minValue;
        set { _minValue = value; OnPropertyChanged(); }
    }

    /// <summary>数字输入模式下允许的最大值；为 null 时不限制上限。</summary>
    public double? MaxValue
    {
        get => _maxValue;
        set { _maxValue = value; OnPropertyChanged(); }
    }

    /// <summary>用于验证输入内容的正则表达式图案；为空字符串时不进行正则校验。</summary>
    public string ValidationPattern
    {
        get => _validationPattern;
        set { _validationPattern = value; OnPropertyChanged(); }
    }
}
