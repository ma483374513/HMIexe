using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using HMIexe.App.ViewModels;
using HMIexe.Core.Models.Controls;
using HMIexe.Core.UndoRedo;

namespace HMIexe.App.Views;

public partial class DesignerView : UserControl
{
    // Drag state
    private bool _isDragging;
    private Point _dragStartPoint;
    private List<(HmiControlBase Ctrl, double OrigX, double OrigY)> _dragItems = new();

    public DesignerView()
    {
        InitializeComponent();

        // Use tunneling to intercept pointer events before child controls consume them
        DesignCanvas.AddHandler(PointerPressedEvent, OnCanvasPointerPressed, RoutingStrategies.Tunnel);
        DesignCanvas.AddHandler(PointerMovedEvent, OnCanvasPointerMoved, RoutingStrategies.Tunnel);
        DesignCanvas.AddHandler(PointerReleasedEvent, OnCanvasPointerReleased, RoutingStrategies.Tunnel);
        DesignCanvas.AddHandler(PointerWheelChangedEvent, OnCanvasPointerWheel, RoutingStrategies.Tunnel);
    }

    private DesignerViewModel? Vm => DataContext as DesignerViewModel;

    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var vm = Vm;
        if (vm?.CurrentPage == null) return;

        var pos = e.GetCurrentPoint(DesignCanvas).Position;
        bool multiSelect = e.KeyModifiers.HasFlag(KeyModifiers.Control) ||
                           e.KeyModifiers.HasFlag(KeyModifiers.Meta);

        var hit = HitTestControl(vm, pos.X, pos.Y);

        if (hit != null && hit.Locked)
        {
            // Locked controls can be selected but not moved
            vm.SelectControl(hit, multiSelect);
            return;
        }

        vm.SelectControl(hit, multiSelect);

        if (hit != null)
        {
            // Prepare drag: snapshot starting positions of ALL selected controls
            _dragStartPoint = pos;
            _dragItems = vm.SelectedControls
                .Select(c => (c, c.X, c.Y))
                .ToList();
            _isDragging = false; // will become true on first move > threshold
            e.Pointer.Capture(DesignCanvas);
        }

        // Focus the designer so keyboard events work
        Focus();
        e.Handled = true;
    }

    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        var pos = e.GetCurrentPoint(DesignCanvas).Position;
        var vm = Vm;
        if (vm != null)
        {
            vm.CanvasMouseX = Math.Round(pos.X);
            vm.CanvasMouseY = Math.Round(pos.Y);
        }

        if (_dragItems.Count == 0) return;
        if (vm == null) return;

        var dx = pos.X - _dragStartPoint.X;
        var dy = pos.Y - _dragStartPoint.Y;

        // Only start dragging after a small threshold to avoid accidental moves
        if (!_isDragging && (Math.Abs(dx) < 3 && Math.Abs(dy) < 3)) return;
        _isDragging = true;

        foreach (var (ctrl, origX, origY) in _dragItems)
        {
            var newX = origX + dx;
            var newY = origY + dy;

            if (vm.SnapToGrid && vm.GridSize > 0)
            {
                newX = Math.Round(newX / vm.GridSize) * vm.GridSize;
                newY = Math.Round(newY / vm.GridSize) * vm.GridSize;
            }

            ctrl.X = Math.Max(0, newX);
            ctrl.Y = Math.Max(0, newY);
        }

        e.Handled = true;
    }

    private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDragging && _dragItems.Count > 0)
        {
            var vm = Vm;
            if (vm != null)
            {
                // Commit to undo history: compare original vs final positions
                var moves = _dragItems
                    .Where(item => Math.Abs(item.Ctrl.X - item.OrigX) > 0.001 ||
                                   Math.Abs(item.Ctrl.Y - item.OrigY) > 0.001)
                    .Select(item => (item.Ctrl, item.OrigX, item.OrigY, item.Ctrl.X, item.Ctrl.Y))
                    .ToList();

                if (moves.Count > 0)
                    vm.UndoRedo.Push(new MoveMultipleControlsAction(moves));
            }
        }

        _isDragging = false;
        _dragItems.Clear();
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        var vm = Vm;
        if (vm == null) return;

        // Use ControlOrMeta to support both Windows/Linux (Ctrl) and macOS (Cmd)
        bool ctrlOrCmd = e.KeyModifiers.HasFlag(KeyModifiers.Control) ||
                         e.KeyModifiers.HasFlag(KeyModifiers.Meta);

        switch (e.Key)
        {
            case Key.Delete:
            case Key.Back:
                vm.DeleteSelectedControlsCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.C when ctrlOrCmd:
                vm.CopySelectedControlsCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.V when ctrlOrCmd:
                vm.PasteControlsCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Z when ctrlOrCmd:
                vm.UndoCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Y when ctrlOrCmd:
                vm.RedoCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.A when ctrlOrCmd:
                SelectAll(vm);
                e.Handled = true;
                break;
        }
    }

    private static void SelectAll(DesignerViewModel vm)
    {
        if (vm.CurrentPage == null) return;
        var all = vm.CurrentPage.AllControls.ToList();
        vm.SelectControl(null, false);
        foreach (var c in all)
            vm.SelectControl(c, true);
    }

    private static HmiControlBase? HitTestControl(DesignerViewModel vm, double x, double y)
    {
        if (vm.CurrentPage == null) return null;
        // Return the topmost (highest ZIndex) visible control that contains the point
        return vm.CurrentPage.AllControls
            .Where(c => c.Visible && x >= c.X && x <= c.X + c.Width && y >= c.Y && y <= c.Y + c.Height)
            .OrderByDescending(c => c.ZIndex)
            .FirstOrDefault();
    }

    private void OnCanvasPointerWheel(object? sender, PointerWheelEventArgs e)
    {
        var vm = Vm;
        if (vm == null) return;
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) ||
            e.KeyModifiers.HasFlag(KeyModifiers.Meta))
        {
            var zoomDelta = e.Delta.Y > 0 ? 0.1 : -0.1;
            vm.ZoomLevel = Math.Clamp(vm.ZoomLevel + zoomDelta, 0.1, 5.0);
            e.Handled = true;
        }
    }
}
