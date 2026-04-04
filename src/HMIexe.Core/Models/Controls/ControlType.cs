/// <summary>
/// HMI 控件类型枚举。
/// 枚举系统中所有可用的控件种类，用于在序列化、反序列化和工厂方法中识别控件类型。
/// </summary>
namespace HMIexe.Core.Models.Controls;

/// <summary>
/// HMI 控件类型，标识控件的种类以便运行时正确实例化与渲染。
/// </summary>
public enum ControlType
{
    /// <summary>按钮控件，支持点击事件和页面跳转。</summary>
    Button,
    /// <summary>标签控件，用于显示静态或动态文本。</summary>
    Label,
    /// <summary>文本输入框控件，支持文本、密码和数字输入模式。</summary>
    TextBox,
    /// <summary>图片控件，显示本地资源或路径指定的图像。</summary>
    Image,
    /// <summary>动画控件，播放序列帧或矢量动画。</summary>
    Animation,
    /// <summary>指示灯控件，通过颜色变化反映开关量状态。</summary>
    IndicatorLight,
    /// <summary>开关控件，提供拨动开关交互，用于切换布尔状态。</summary>
    Switch,
    /// <summary>滑块控件，通过拖动设置连续数值。</summary>
    Slider,
    /// <summary>仪表盘控件，以指针/弧形可视化模拟量数值。</summary>
    Gauge,
    /// <summary>直线控件，绘制一条线段。</summary>
    Line,
    /// <summary>折线控件，绘制由多个顶点连接的折线。</summary>
    Polyline,
    /// <summary>矩形控件，绘制矩形或圆角矩形。</summary>
    Rectangle,
    /// <summary>圆形控件，绘制正圆形。</summary>
    Circle,
    /// <summary>椭圆控件，绘制任意比例的椭圆。</summary>
    Ellipse,
    /// <summary>弧形控件，绘制圆弧段。</summary>
    Arc,
    /// <summary>环形控件，绘制空心圆环。</summary>
    Ring,
    /// <summary>多边形控件，绘制任意多边形。</summary>
    Polygon,
    /// <summary>组合控件，将多个控件打包为一个可整体操作的组。</summary>
    Group,
    /// <summary>自定义控件，用于扩展系统内置控件类型之外的特殊控件。</summary>
    Custom
}
