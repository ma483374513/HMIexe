using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HMIexe.Core.Models.Canvas;
using HMIexe.Core.Models.Controls;
using HMIexe.Core.Models.Project;
using HMIexe.Core.UndoRedo;

namespace HMIexe.App.ViewModels;

public partial class DesignerViewModel : ObservableObject
{
    public UndoRedoHistory UndoRedo { get; } = new();
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

    // Clipboard for copy/paste
    private string? _clipboardJson;

    public ObservableCollection<HmiControlBase> SelectedControls { get; } = new();
    public ObservableCollection<HmiPage> Pages { get; } = new();

    [RelayCommand]
    private void Undo() => UndoRedo.Undo();

    [RelayCommand]
    private void Redo() => UndoRedo.Redo();

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
    private void AddLayer()
    {
        if (CurrentPage == null) return;
        var layer = new HmiLayer { Name = $"Layer {CurrentPage.Layers.Count + 1}" };
        CurrentPage.Layers.Add(layer);
    }

    /// <summary>Select or add to selection. Pass null to deselect all.</summary>
    public void SelectControl(HmiControlBase? ctrl, bool addToSelection)
    {
        if (!addToSelection)
        {
            foreach (var c in SelectedControls) c.IsSelected = false;
            SelectedControls.Clear();
        }

        if (ctrl == null)
        {
            SelectedControl = null;
            PropertyPanel.SelectedControl = null;
            return;
        }

        if (!SelectedControls.Contains(ctrl))
        {
            ctrl.IsSelected = true;
            SelectedControls.Add(ctrl);
        }

        SelectedControl = ctrl;
        PropertyPanel.SelectedControl = ctrl;
    }

    [RelayCommand]
    private void DeleteSelectedControls()
    {
        if (CurrentPage == null) return;
        var items = SelectedControls
            .SelectMany(ctrl => CurrentPage.Layers
                .Where(l => l.Controls.Contains(ctrl))
                .Select(l => (l, ctrl)))
            .ToList();
        if (items.Count == 0) return;
        var action = new RemoveControlsAction(items);
        UndoRedo.Execute(action);
        foreach (var c in SelectedControls) c.IsSelected = false;
        SelectedControls.Clear();
        SelectedControl = null;
        PropertyPanel.SelectedControl = null;
    }

    [RelayCommand]
    private void CopySelectedControls()
    {
        if (SelectedControls.Count == 0) return;
        var list = SelectedControls.Select(c => new
        {
            TypeName = c.GetType().Name,
            Json = JsonSerializer.Serialize(c, c.GetType())
        }).ToList();
        _clipboardJson = JsonSerializer.Serialize(list);
    }

    [RelayCommand]
    private void PasteControls()
    {
        if (CurrentPage == null || string.IsNullOrEmpty(_clipboardJson)) return;
        var layer = CurrentPage.Layers.FirstOrDefault();
        if (layer == null) return;

        var list = JsonSerializer.Deserialize<List<ClipboardEntry>>(_clipboardJson);
        if (list == null) return;

        // Build a lookup of control types in the Controls namespace
        var controlTypes = typeof(HmiControlBase).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(HmiControlBase)))
            .ToDictionary(t => t.Name, t => t);

        var pasted = new List<HmiControlBase>();
        foreach (var entry in list)
        {
            if (!controlTypes.TryGetValue(entry.TypeName, out var type)) continue;
            if (JsonSerializer.Deserialize(entry.Json, type) is not HmiControlBase ctrl) continue;
            ctrl.Id = Guid.NewGuid().ToString();
            ctrl.X += 20;
            ctrl.Y += 20;
            ctrl.IsSelected = false;
            pasted.Add(ctrl);
        }

        if (pasted.Count == 0) return;

        var addActions = pasted.Select(c => new AddControlAction(layer, c)).ToList();
        UndoRedo.Execute(new SetPropertyAction(
            $"粘贴 {pasted.Count} 个控件",
            () => { foreach (var a in addActions) a.Execute(); },
            () => { foreach (var a in addActions) a.Undo(); }
        ));

        foreach (var c in SelectedControls) c.IsSelected = false;
        SelectedControls.Clear();
        foreach (var c in pasted)
        {
            c.IsSelected = true;
            SelectedControls.Add(c);
        }
        SelectedControl = pasted.LastOrDefault();
        PropertyPanel.SelectedControl = SelectedControl;
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

    public PropertyPanelViewModel PropertyPanel { get; }

    public DesignerViewModel()
    {
        PropertyPanel = new PropertyPanelViewModel(UndoRedo);
    }

    partial void OnSelectedControlChanged(HmiControlBase? value)
    {
        PropertyPanel.SelectedControl = value;
    }

    public void LoadProject(HmiProject project)
    {
        Pages.Clear();
        foreach (var c in SelectedControls) c.IsSelected = false;
        SelectedControls.Clear();
        SelectedControl = null;
        UndoRedo.Clear();

        foreach (var page in project.Pages)
            Pages.Add(page);

        CurrentPage = Pages.FirstOrDefault(p => p.Id == project.DefaultPageId)
            ?? Pages.FirstOrDefault();
    }

    [RelayCommand]
    private void SelectPage(HmiPage page)
    {
        CurrentPage = page;
        foreach (var c in SelectedControls) c.IsSelected = false;
        SelectedControls.Clear();
        SelectedControl = null;
    }

    [RelayCommand]
    private void AddControlToCanvas(string controlTypeStr)
    {
        if (CurrentPage == null) return;
        var layer = CurrentPage.Layers.FirstOrDefault();
        if (layer == null) return;

        HmiControlBase? control = controlTypeStr switch
        {
            "Button" => new Core.Models.Controls.ButtonControl { Name = $"Button{layer.Controls.Count + 1}" },
            "Label" => new Core.Models.Controls.LabelControl { Name = $"Label{layer.Controls.Count + 1}" },
            "TextBox" => new Core.Models.Controls.TextBoxControl { Name = $"TextBox{layer.Controls.Count + 1}" },
            "Gauge" => new Core.Models.Controls.GaugeControl { Name = $"Gauge{layer.Controls.Count + 1}" },
            "IndicatorLight" => new Core.Models.Controls.IndicatorLightControl { Name = $"Light{layer.Controls.Count + 1}" },
            "Switch" => new Core.Models.Controls.SwitchControl { Name = $"Switch{layer.Controls.Count + 1}" },
            "Slider" => new Core.Models.Controls.SliderControl { Name = $"Slider{layer.Controls.Count + 1}" },
            "Line" => new Core.Models.Controls.LineControl { Name = $"Line{layer.Controls.Count + 1}" },
            "Rectangle" => new Core.Models.Controls.RectangleControl { Name = $"Rect{layer.Controls.Count + 1}" },
            "Circle" => new Core.Models.Controls.CircleControl { Name = $"Circle{layer.Controls.Count + 1}" },
            _ => null
        };

        if (control == null) return;
        control.X = 100 + layer.Controls.Count * 10;
        control.Y = 100 + layer.Controls.Count * 10;
        var action = new AddControlAction(layer, control);
        UndoRedo.Execute(action);
        SelectControl(control, false);
    }

    private record ClipboardEntry(string TypeName, string Json);
}
