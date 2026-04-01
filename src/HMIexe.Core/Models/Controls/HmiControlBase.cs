using System.ComponentModel;

namespace HMIexe.Core.Models.Controls;

public abstract class HmiControlBase : INotifyPropertyChanged
{
    private double _x, _y, _width = 100, _height = 40;
    private bool _visible = true, _locked;
    private double _opacity = 1.0;
    private int _zIndex;
    private string _name = string.Empty;

    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public abstract ControlType ControlType { get; }

    public double X
    {
        get => _x;
        set { _x = value; OnPropertyChanged(); }
    }

    public double Y
    {
        get => _y;
        set { _y = value; OnPropertyChanged(); }
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

    public bool Visible
    {
        get => _visible;
        set { _visible = value; OnPropertyChanged(); }
    }

    public bool Locked
    {
        get => _locked;
        set { _locked = value; OnPropertyChanged(); }
    }

    public double Opacity
    {
        get => _opacity;
        set { _opacity = Math.Clamp(value, 0, 1); OnPropertyChanged(); }
    }

    public int ZIndex
    {
        get => _zIndex;
        set { _zIndex = value; OnPropertyChanged(); }
    }

    public string LayerId { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public Dictionary<string, object?> ExtendedProperties { get; set; } = new();
    public Dictionary<string, string> DataBindings { get; set; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
