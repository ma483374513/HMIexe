/// <summary>
/// 图片控件模型。
/// 继承自 HmiControlBase，在画布指定位置显示图像资源，支持多种拉伸填充模式。
/// 默认宽高均为 120px。
/// </summary>
namespace HMIexe.Core.Models.Controls;

/// <summary>
/// HMI 图片控件，用于在画布上显示静态或动态图像资源。
/// </summary>
public class ImageControl : HmiControlBase
{
    // 私有字段：图像资源名称或文件路径
    private string _imagePath = string.Empty;
    // 私有字段：图像拉伸模式，默认为等比缩放（Uniform）
    private string _stretch = "Uniform";

    /// <summary>控件类型标识，固定返回 ControlType.Image。</summary>
    public override ControlType ControlType => ControlType.Image;

    /// <summary>Resource name or file path.</summary>
    public string ImagePath { get => _imagePath; set { _imagePath = value; OnPropertyChanged(); } }

    /// <summary>Stretch mode: None, Fill, Uniform, UniformToFill.</summary>
    public string Stretch { get => _stretch; set { _stretch = value; OnPropertyChanged(); } }

    /// <summary>初始化图片控件，默认宽高均为 120px。</summary>
    public ImageControl()
    {
        Width = 120;
        Height = 120;
    }
}
