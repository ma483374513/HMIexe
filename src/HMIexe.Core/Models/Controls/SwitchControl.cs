/// <summary>
/// 开关控件模型。
/// 继承自 HmiControlBase，提供拨动开关交互，用于切换布尔（开/关）状态，
/// 支持亮灭颜色、标签文字及切换脚本配置。
/// 默认宽 80px、高 36px。
/// </summary>
namespace HMIexe.Core.Models.Controls;

/// <summary>
/// HMI 开关控件，以拨动开关的形式切换布尔量状态，可配置切换脚本实现联动。
/// </summary>
public class SwitchControl : HmiControlBase
{
    // 私有字段：当前开关状态
    private bool _isOn;
    // 私有字段：打开状态的标签文字
    private string _onLabel = "ON";
    // 私有字段：关闭状态的标签文字
    private string _offLabel = "OFF";
    // 私有字段：打开状态的颜色，默认绿色
    private string _onColor = "#4CAF50";
    // 私有字段：关闭状态的颜色，默认灰色
    private string _offColor = "#9E9E9E";
    // 私有字段：切换时执行的脚本代码
    private string _toggleScript = string.Empty;

    /// <summary>控件类型标识，固定返回 ControlType.Switch。</summary>
    public override ControlType ControlType => ControlType.Switch;

    /// <summary>开关的当前状态：true 为打开（ON），false 为关闭（OFF）。</summary>
    public bool IsOn { get => _isOn; set { _isOn = value; OnPropertyChanged(); } }

    /// <summary>打开（ON）状态时显示的标签文字，默认为 "ON"。</summary>
    public string OnLabel { get => _onLabel; set { _onLabel = value; OnPropertyChanged(); } }

    /// <summary>关闭（OFF）状态时显示的标签文字，默认为 "OFF"。</summary>
    public string OffLabel { get => _offLabel; set { _offLabel = value; OnPropertyChanged(); } }

    /// <summary>打开（ON）状态的背景颜色，默认为绿色 "#4CAF50"。</summary>
    public string OnColor { get => _onColor; set { _onColor = value; OnPropertyChanged(); } }

    /// <summary>关闭（OFF）状态的背景颜色，默认为灰色 "#9E9E9E"。</summary>
    public string OffColor { get => _offColor; set { _offColor = value; OnPropertyChanged(); } }

    /// <summary>开关拨动时执行的脚本代码，可用于写变量或触发联动逻辑。</summary>
    public string ToggleScript { get => _toggleScript; set { _toggleScript = value; OnPropertyChanged(); } }

    /// <summary>初始化开关控件，默认宽 80px、高 36px。</summary>
    public SwitchControl()
    {
        Width = 80;
        Height = 36;
    }
}
