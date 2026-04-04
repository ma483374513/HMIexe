using System.ComponentModel;
using System.Text.Json.Serialization;

/// <summary>
/// HMI 画布页面模型。
/// 定义页面切换动画类型枚举及页面本身的属性，包括尺寸、背景、图层集合与生命周期脚本。
/// 实现 INotifyPropertyChanged 以支持 UI 数据绑定。
/// </summary>
namespace HMIexe.Core.Models.Canvas;

/// <summary>
/// 页面切换动画类型枚举。
/// 指定在两个页面之间跳转时使用的过渡效果。
/// </summary>
public enum PageTransitionType
{
    /// <summary>无过渡动画，直接切换。</summary>
    None,
    /// <summary>淡入淡出效果。</summary>
    Fade,
    /// <summary>向左滑动效果。</summary>
    SlideLeft,
    /// <summary>向右滑动效果。</summary>
    SlideRight,
    /// <summary>向上滑动效果。</summary>
    SlideUp,
    /// <summary>向下滑动效果。</summary>
    SlideDown,
    /// <summary>缩放效果。</summary>
    Zoom
}

/// <summary>
/// HMI 页面，表示项目中的一个独立画面。
/// 每个页面包含若干图层，每个图层再包含具体控件。
/// 页面支持背景颜色/图片、切换动画及加载/关闭脚本。
/// </summary>
public class HmiPage : INotifyPropertyChanged
{
    // 私有字段：页面名称
    private string _name = "Page";
    // 私有字段：页面宽度，默认 1920px（适配全高清分辨率）
    private double _width = 1920;
    // 私有字段：页面高度，默认 1080px
    private double _height = 1080;
    // 私有字段：背景颜色，默认白色
    private string _backgroundColor = "#FFFFFF";
    // 私有字段：是否为项目启动时的默认页面
    private bool _isDefault;
    // 私有字段：切换动画类型，默认淡入淡出
    private PageTransitionType _transitionType = PageTransitionType.Fade;
    // 私有字段：切换动画时长（秒），默认 0.3 秒
    private double _transitionDuration = 0.3;

    /// <summary>页面的唯一标识符（GUID）。</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>页面名称，显示在页面管理列表中。</summary>
    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    /// <summary>页面宽度（像素），默认为 1920。</summary>
    public double Width
    {
        get => _width;
        set { _width = value; OnPropertyChanged(); }
    }

    /// <summary>页面高度（像素），默认为 1080。</summary>
    public double Height
    {
        get => _height;
        set { _height = value; OnPropertyChanged(); }
    }

    /// <summary>页面背景颜色，十六进制 ARGB/RGB 字符串，默认为白色 "#FFFFFF"。</summary>
    public string BackgroundColor
    {
        get => _backgroundColor;
        set { _backgroundColor = value; OnPropertyChanged(); }
    }

    /// <summary>是否为项目的默认首页；项目启动时将首先导航到此页面。</summary>
    public bool IsDefault
    {
        get => _isDefault;
        set { _isDefault = value; OnPropertyChanged(); }
    }

    /// <summary>页面切换时使用的过渡动画类型。</summary>
    public PageTransitionType TransitionType
    {
        get => _transitionType;
        set { _transitionType = value; OnPropertyChanged(); }
    }

    /// <summary>页面切换动画的持续时间（秒），默认为 0.3 秒。</summary>
    public double TransitionDuration
    {
        get => _transitionDuration;
        set { _transitionDuration = value; OnPropertyChanged(); }
    }

    /// <summary>背景图片的资源路径；为 null 时不显示背景图。</summary>
    public string? BackgroundImagePath { get; set; }

    /// <summary>页面包含的图层列表，图层按 Order 属性顺序叠加渲染。</summary>
    public List<HmiLayer> Layers { get; set; } = new();

    /// <summary>与页面关联的脚本字典（键：脚本名称，值：脚本代码）。</summary>
    public Dictionary<string, string> Scripts { get; set; } = new();

    /// <summary>页面加载完成后自动执行的脚本代码。</summary>
    public string OnLoadScript { get; set; } = string.Empty;

    /// <summary>页面关闭/离开时自动执行的脚本代码。</summary>
    public string OnCloseScript { get; set; } = string.Empty;

    /// <summary>
    /// 获取该页面所有图层中全部控件的扁平枚举。
    /// 此属性不参与 JSON 序列化（仅用于运行时查询）。
    /// </summary>
    [JsonIgnore]
    public IEnumerable<Controls.HmiControlBase> AllControls =>
        Layers.SelectMany(l => l.Controls);

    /// <summary>属性变更通知事件，用于 WPF/MAUI 数据绑定刷新。</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>触发属性变更通知；由编译器自动填充调用者属性名。</summary>
    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
