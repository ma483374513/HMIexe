namespace HMIexe.Core.Models.Controls;

public class ImageControl : HmiControlBase
{
    private string _imagePath = string.Empty;
    private string _stretch = "Uniform";

    public override ControlType ControlType => ControlType.Image;

    /// <summary>Resource name or file path.</summary>
    public string ImagePath { get => _imagePath; set { _imagePath = value; OnPropertyChanged(); } }

    /// <summary>Stretch mode: None, Fill, Uniform, UniformToFill.</summary>
    public string Stretch { get => _stretch; set { _stretch = value; OnPropertyChanged(); } }

    public ImageControl()
    {
        Width = 120;
        Height = 120;
    }
}
