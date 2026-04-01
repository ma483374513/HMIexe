using CommunityToolkit.Mvvm.ComponentModel;
using HMIexe.Core.Models.Controls;

namespace HMIexe.App.ViewModels;

public partial class PropertyPanelViewModel : ObservableObject
{
    [ObservableProperty]
    private HmiControlBase? _selectedControl;

    [ObservableProperty]
    private string _searchText = string.Empty;

    partial void OnSelectedControlChanged(HmiControlBase? value)
    {
        OnPropertyChanged(nameof(ControlName));
        OnPropertyChanged(nameof(ControlX));
        OnPropertyChanged(nameof(ControlY));
        OnPropertyChanged(nameof(ControlWidth));
        OnPropertyChanged(nameof(ControlHeight));
        OnPropertyChanged(nameof(ControlVisible));
        OnPropertyChanged(nameof(ControlLocked));
        OnPropertyChanged(nameof(ControlOpacity));
        OnPropertyChanged(nameof(ControlZIndex));
    }

    public string ControlName
    {
        get => SelectedControl?.Name ?? string.Empty;
        set { if (SelectedControl != null) SelectedControl.Name = value; }
    }

    public double ControlX
    {
        get => SelectedControl?.X ?? 0;
        set { if (SelectedControl != null) SelectedControl.X = value; }
    }

    public double ControlY
    {
        get => SelectedControl?.Y ?? 0;
        set { if (SelectedControl != null) SelectedControl.Y = value; }
    }

    public double ControlWidth
    {
        get => SelectedControl?.Width ?? 0;
        set { if (SelectedControl != null && value > 0) SelectedControl.Width = value; }
    }

    public double ControlHeight
    {
        get => SelectedControl?.Height ?? 0;
        set { if (SelectedControl != null && value > 0) SelectedControl.Height = value; }
    }

    public bool ControlVisible
    {
        get => SelectedControl?.Visible ?? true;
        set { if (SelectedControl != null) SelectedControl.Visible = value; }
    }

    public bool ControlLocked
    {
        get => SelectedControl?.Locked ?? false;
        set { if (SelectedControl != null) SelectedControl.Locked = value; }
    }

    public double ControlOpacity
    {
        get => SelectedControl?.Opacity ?? 1.0;
        set { if (SelectedControl != null) SelectedControl.Opacity = value; }
    }

    public int ControlZIndex
    {
        get => SelectedControl?.ZIndex ?? 0;
        set { if (SelectedControl != null) SelectedControl.ZIndex = value; }
    }
}
