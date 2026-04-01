using System.ComponentModel;
using HMIexe.Core.Models.Controls;

namespace HMIexe.Core.Models.Canvas;

public class HmiLayer : INotifyPropertyChanged
{
    private string _name = "Layer";
    private bool _visible = true;
    private bool _locked;
    private int _order;

    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
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

    public int Order
    {
        get => _order;
        set { _order = value; OnPropertyChanged(); }
    }

    public List<HmiControlBase> Controls { get; set; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
