using System.ComponentModel;

/// <summary>
/// HMI 控件基类。
/// 所有 HMI 控件均继承自此抽象类，提供位置、尺寸、可见性、透明度、层级、
/// 数据绑定及属性变更通知等公共能力。
/// </summary>
namespace HMIexe.Core.Models.Controls;

/// <summary>
/// HMI 控件抽象基类，定义所有控件共有的属性和行为。
/// 派生类必须实现 <see cref="ControlType"/> 以声明自身控件类型。
/// 实现 <see cref="INotifyPropertyChanged"/> 以支持 UI 数据绑定。
/// </summary>
public abstract class HmiControlBase : INotifyPropertyChanged
{
    // 私有字段：位置与尺寸，默认宽 100、高 40
    private double _x, _y, _width = 100, _height = 40;
    // 私有字段：可见性（默认可见）和锁定状态（默认未锁定）
    private bool _visible = true, _locked;
    // 私有字段：透明度，范围 [0,1]，默认完全不透明
    private double _opacity = 1.0;
    // 私有字段：Z 轴层级顺序
    private int _zIndex;
    // 私有字段：控件名称
    private string _name = string.Empty;

    /// <summary>控件的唯一标识符（GUID），在项目中全局唯一。</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>控件名称，显示在属性面板和图层树中，用于识别控件。</summary>
    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    /// <summary>控件类型，由派生类实现，返回对应的 <see cref="Controls.ControlType"/> 枚举值。</summary>
    public abstract ControlType ControlType { get; }

    /// <summary>控件在画布上的 X 坐标（左边缘，相对于页面原点）。</summary>
    public double X
    {
        get => _x;
        set { _x = value; OnPropertyChanged(); }
    }

    /// <summary>控件在画布上的 Y 坐标（上边缘，相对于页面原点）。</summary>
    public double Y
    {
        get => _y;
        set { _y = value; OnPropertyChanged(); }
    }

    /// <summary>控件宽度（像素），默认为 100。</summary>
    public double Width
    {
        get => _width;
        set { _width = value; OnPropertyChanged(); }
    }

    /// <summary>控件高度（像素），默认为 40。</summary>
    public double Height
    {
        get => _height;
        set { _height = value; OnPropertyChanged(); }
    }

    /// <summary>控件是否可见；设为 false 时控件不渲染但仍存在于模型中。</summary>
    public bool Visible
    {
        get => _visible;
        set { _visible = value; OnPropertyChanged(); }
    }

    /// <summary>控件是否锁定；锁定后在设计器中无法选中或移动该控件。</summary>
    public bool Locked
    {
        get => _locked;
        set { _locked = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 控件透明度，范围 [0.0, 1.0]，0 为完全透明，1 为完全不透明。
    /// 赋值时自动裁剪到合法范围。
    /// </summary>
    public double Opacity
    {
        get => _opacity;
        set { _opacity = Math.Clamp(value, 0, 1); OnPropertyChanged(); }
    }

    /// <summary>控件在图层内的 Z 轴叠放顺序，数值越大越靠前显示。</summary>
    public int ZIndex
    {
        get => _zIndex;
        set { _zIndex = value; OnPropertyChanged(); }
    }

    // 仅运行时使用，不参与持久化序列化
    private bool _isSelected;

    /// <summary>
    /// 控件是否被设计器选中（仅运行时状态，不持久化）。
    /// 用于在设计器画布中高亮显示选中的控件。
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    private string _valueBindingVariable = string.Empty;

    /// <summary>运行时绑定到控件主值属性的变量名称，用于将 HMI 变量值动态映射到控件的主要显示属性。</summary>
    public string ValueBindingVariable
    {
        get => _valueBindingVariable;
        set { _valueBindingVariable = value; OnPropertyChanged(); }
    }

    /// <summary>该控件所属图层的 ID，用于确定控件在页面图层结构中的位置。</summary>
    public string LayerId { get; set; } = string.Empty;

    /// <summary>该控件所属组合控件的 ID；未分组时为空字符串。</summary>
    public string GroupId { get; set; } = string.Empty;

    /// <summary>扩展属性字典，用于存储派生控件或插件定义的额外属性。</summary>
    public Dictionary<string, object?> ExtendedProperties { get; set; } = new();

    /// <summary>数据绑定字典（键：属性名，值：变量名），定义控件属性与 HMI 变量之间的运行时绑定关系。</summary>
    public Dictionary<string, string> DataBindings { get; set; } = new();

    /// <summary>属性变更通知事件，用于 WPF/MAUI 数据绑定刷新。</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>触发属性变更通知；由编译器自动填充调用者属性名。</summary>
    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
