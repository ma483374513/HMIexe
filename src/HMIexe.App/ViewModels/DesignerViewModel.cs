using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HMIexe.Core.Models.Canvas;
using HMIexe.Core.Models.Controls;

namespace HMIexe.App.ViewModels;

public partial class DesignerViewModel : ObservableObject
{
    [ObservableProperty]
    private HmiPage? _currentPage;

    [ObservableProperty]
    private HmiControlBase? _selectedControl;

    [ObservableProperty]
    private double _zoomLevel = 1.0;

    [ObservableProperty]
    private double _panOffsetX;

    [ObservableProperty]
    private double _panOffsetY;

    [ObservableProperty]
    private bool _showGrid = true;

    [ObservableProperty]
    private double _gridSize = 10;

    [ObservableProperty]
    private bool _snapToGrid = true;

    public ObservableCollection<HmiControlBase> SelectedControls { get; } = new();
    public ObservableCollection<HmiPage> Pages { get; } = new();

    [RelayCommand]
    private void ZoomIn() => ZoomLevel = Math.Min(ZoomLevel + 0.1, 5.0);

    [RelayCommand]
    private void ZoomOut() => ZoomLevel = Math.Max(ZoomLevel - 0.1, 0.1);

    [RelayCommand]
    private void ResetZoom() => ZoomLevel = 1.0;

    [RelayCommand]
    private void AddPage()
    {
        var page = new HmiPage { Name = $"Page {Pages.Count + 1}" };
        page.Layers.Add(new HmiLayer { Name = "Layer 1" });
        Pages.Add(page);
        CurrentPage = page;
    }

    [RelayCommand]
    private void DeleteSelectedControls()
    {
        if (CurrentPage == null) return;
        foreach (var control in SelectedControls.ToList())
        {
            foreach (var layer in CurrentPage.Layers)
                layer.Controls.Remove(control);
        }
        SelectedControls.Clear();
        SelectedControl = null;
    }

    [RelayCommand]
    private void AlignLeft()
    {
        if (SelectedControls.Count < 2) return;
        var minX = SelectedControls.Min(c => c.X);
        foreach (var c in SelectedControls) c.X = minX;
    }

    [RelayCommand]
    private void AlignRight()
    {
        if (SelectedControls.Count < 2) return;
        var maxRight = SelectedControls.Max(c => c.X + c.Width);
        foreach (var c in SelectedControls) c.X = maxRight - c.Width;
    }

    [RelayCommand]
    private void AlignTop()
    {
        if (SelectedControls.Count < 2) return;
        var minY = SelectedControls.Min(c => c.Y);
        foreach (var c in SelectedControls) c.Y = minY;
    }

    [RelayCommand]
    private void AlignBottom()
    {
        if (SelectedControls.Count < 2) return;
        var maxBottom = SelectedControls.Max(c => c.Y + c.Height);
        foreach (var c in SelectedControls) c.Y = maxBottom - c.Height;
    }

    [RelayCommand]
    private void AlignCenterHorizontal()
    {
        if (SelectedControls.Count < 2) return;
        var avgCenterY = SelectedControls.Average(c => c.Y + c.Height / 2);
        foreach (var c in SelectedControls) c.Y = avgCenterY - c.Height / 2;
    }

    [RelayCommand]
    private void AlignCenterVertical()
    {
        if (SelectedControls.Count < 2) return;
        var avgCenterX = SelectedControls.Average(c => c.X + c.Width / 2);
        foreach (var c in SelectedControls) c.X = avgCenterX - c.Width / 2;
    }

    [RelayCommand]
    private void DistributeHorizontally()
    {
        if (SelectedControls.Count < 3) return;
        var sorted = SelectedControls.OrderBy(c => c.X).ToList();
        var totalWidth = sorted.Last().X + sorted.Last().Width - sorted.First().X;
        var controlsWidth = sorted.Sum(c => c.Width);
        var gap = (totalWidth - controlsWidth) / (sorted.Count - 1);
        var x = sorted.First().X;
        foreach (var c in sorted)
        {
            c.X = x;
            x += c.Width + gap;
        }
    }

    [RelayCommand]
    private void DistributeVertically()
    {
        if (SelectedControls.Count < 3) return;
        var sorted = SelectedControls.OrderBy(c => c.Y).ToList();
        var totalHeight = sorted.Last().Y + sorted.Last().Height - sorted.First().Y;
        var controlsHeight = sorted.Sum(c => c.Height);
        var gap = (totalHeight - controlsHeight) / (sorted.Count - 1);
        var y = sorted.First().Y;
        foreach (var c in sorted)
        {
            c.Y = y;
            y += c.Height + gap;
        }
    }
}
