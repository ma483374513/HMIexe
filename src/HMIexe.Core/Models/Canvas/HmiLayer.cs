using System.ComponentModel;
using HMIexe.Core.Models.Controls;

/// <summary>
/// HMI 画布图层模型。
/// 图层是页面中控件的分组容器，支持可见性切换、锁定保护及叠放顺序管理。
/// 实现 INotifyPropertyChanged 以支持 UI 数据绑定。
/// </summary>
namespace HMIexe.Core.Models.Canvas;

/// <summary>
/// HMI 图层，表示页面中的一个控件分组层。
/// 每个图层拥有独立的可见性、锁定状态和叠放顺序，包含零至多个 HMI 控件。
/// </summary>
public class HmiLayer : INotifyPropertyChanged
{
    // 私有字段：图层名称，默认值为 "Layer"
    private string _name = "Layer";
    // 私有字段：可见性，默认可见
    private bool _visible = true;
    // 私有字段：锁定状态，默认未锁定
    private bool _locked;
    // 私有字段：图层叠放顺序
    private int _order;

    /// <summary>图层的唯一标识符（GUID）。</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>图层名称，显示在图层面板中供设计者识别。</summary>
    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    /// <summary>图层是否可见；隐藏时该图层内所有控件均不渲染。</summary>
    public bool Visible
    {
        get => _visible;
        set { _visible = value; OnPropertyChanged(); }
    }

    /// <summary>图层是否锁定；锁定后禁止在设计器中选中或移动该图层内的控件。</summary>
    public bool Locked
    {
        get => _locked;
        set { _locked = value; OnPropertyChanged(); }
    }

    /// <summary>图层的叠放顺序，数值越大越靠上显示。</summary>
    public int Order
    {
        get => _order;
        set { _order = value; OnPropertyChanged(); }
    }

    /// <summary>该图层包含的所有 HMI 控件列表。</summary>
    public List<HmiControlBase> Controls { get; set; } = new();

    /// <summary>属性变更通知事件，用于 WPF/MAUI 数据绑定刷新。</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>触发属性变更通知；由编译器自动填充调用者属性名。</summary>
    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
