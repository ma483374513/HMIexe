using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HMIexe.Core.Models.Canvas;

public enum PageTransitionType { None, Fade, SlideLeft, SlideRight, SlideUp, SlideDown, Zoom }

public class HmiPage : INotifyPropertyChanged
{
    private string _name = "Page";
    private double _width = 1920;
    private double _height = 1080;
    private string _backgroundColor = "#FFFFFF";
    private bool _isDefault;
    private PageTransitionType _transitionType = PageTransitionType.Fade;
    private double _transitionDuration = 0.3;

    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public double Width
    {
        get => _width;
        set { _width = value; OnPropertyChanged(); }
    }

    public double Height
    {
        get => _height;
        set { _height = value; OnPropertyChanged(); }
    }

    public string BackgroundColor
    {
        get => _backgroundColor;
        set { _backgroundColor = value; OnPropertyChanged(); }
    }

    public bool IsDefault
    {
        get => _isDefault;
        set { _isDefault = value; OnPropertyChanged(); }
    }

    public PageTransitionType TransitionType
    {
        get => _transitionType;
        set { _transitionType = value; OnPropertyChanged(); }
    }

    public double TransitionDuration
    {
        get => _transitionDuration;
        set { _transitionDuration = value; OnPropertyChanged(); }
    }

    public string? BackgroundImagePath { get; set; }
    public List<HmiLayer> Layers { get; set; } = new();
    public Dictionary<string, string> Scripts { get; set; } = new();
    public string OnLoadScript { get; set; } = string.Empty;
    public string OnCloseScript { get; set; } = string.Empty;

    [JsonIgnore]
    public IEnumerable<Controls.HmiControlBase> AllControls =>
        Layers.SelectMany(l => l.Controls);

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
