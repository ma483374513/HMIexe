using CommunityToolkit.Mvvm.ComponentModel;
using HMIexe.Core.Models.Controls;
using HMIexe.Core.UndoRedo;

namespace HMIexe.App.ViewModels;

public partial class PropertyPanelViewModel : ObservableObject
{
    private readonly UndoRedoHistory _undoRedo;

    [ObservableProperty]
    private HmiControlBase? _selectedControl;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public PropertyPanelViewModel(UndoRedoHistory undoRedo)
    {
        _undoRedo = undoRedo;
    }

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
        set
        {
            if (SelectedControl == null) return;
            var old = SelectedControl.Name;
            if (old == value) return;
            _undoRedo.Execute(new SetPropertyAction($"重命名 → {value}",
                () => SelectedControl.Name = value,
                () => SelectedControl.Name = old));
            OnPropertyChanged();
        }
    }

    public double ControlX
    {
        get => SelectedControl?.X ?? 0;
        set
        {
            if (SelectedControl == null) return;
            var old = SelectedControl.X;
            if (Math.Abs(old - value) < 0.001) return;
            _undoRedo.Execute(new SetPropertyAction("修改 X",
                () => SelectedControl.X = value,
                () => SelectedControl.X = old));
            OnPropertyChanged();
        }
    }

    public double ControlY
    {
        get => SelectedControl?.Y ?? 0;
        set
        {
            if (SelectedControl == null) return;
            var old = SelectedControl.Y;
            if (Math.Abs(old - value) < 0.001) return;
            _undoRedo.Execute(new SetPropertyAction("修改 Y",
                () => SelectedControl.Y = value,
                () => SelectedControl.Y = old));
            OnPropertyChanged();
        }
    }

    public double ControlWidth
    {
        get => SelectedControl?.Width ?? 0;
        set
        {
            if (SelectedControl == null || value <= 0) return;
            var old = SelectedControl.Width;
            if (Math.Abs(old - value) < 0.001) return;
            _undoRedo.Execute(new SetPropertyAction("修改宽度",
                () => SelectedControl.Width = value,
                () => SelectedControl.Width = old));
            OnPropertyChanged();
        }
    }

    public double ControlHeight
    {
        get => SelectedControl?.Height ?? 0;
        set
        {
            if (SelectedControl == null || value <= 0) return;
            var old = SelectedControl.Height;
            if (Math.Abs(old - value) < 0.001) return;
            _undoRedo.Execute(new SetPropertyAction("修改高度",
                () => SelectedControl.Height = value,
                () => SelectedControl.Height = old));
            OnPropertyChanged();
        }
    }

    public bool ControlVisible
    {
        get => SelectedControl?.Visible ?? true;
        set
        {
            if (SelectedControl == null) return;
            var old = SelectedControl.Visible;
            if (old == value) return;
            _undoRedo.Execute(new SetPropertyAction(value ? "设为可见" : "设为隐藏",
                () => SelectedControl.Visible = value,
                () => SelectedControl.Visible = old));
            OnPropertyChanged();
        }
    }

    public bool ControlLocked
    {
        get => SelectedControl?.Locked ?? false;
        set
        {
            if (SelectedControl == null) return;
            var old = SelectedControl.Locked;
            if (old == value) return;
            _undoRedo.Execute(new SetPropertyAction(value ? "锁定" : "解锁",
                () => SelectedControl.Locked = value,
                () => SelectedControl.Locked = old));
            OnPropertyChanged();
        }
    }

    public double ControlOpacity
    {
        get => SelectedControl?.Opacity ?? 1.0;
        set
        {
            if (SelectedControl == null) return;
            var old = SelectedControl.Opacity;
            if (Math.Abs(old - value) < 0.001) return;
            _undoRedo.Execute(new SetPropertyAction("修改透明度",
                () => SelectedControl.Opacity = value,
                () => SelectedControl.Opacity = old));
            OnPropertyChanged();
        }
    }

    public int ControlZIndex
    {
        get => SelectedControl?.ZIndex ?? 0;
        set
        {
            if (SelectedControl == null) return;
            var old = SelectedControl.ZIndex;
            if (old == value) return;
            _undoRedo.Execute(new SetPropertyAction("修改层级",
                () => SelectedControl.ZIndex = value,
                () => SelectedControl.ZIndex = old));
            OnPropertyChanged();
        }
    }
}
