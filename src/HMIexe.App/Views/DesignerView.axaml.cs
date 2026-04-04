/// <summary>
/// HMI 设计器视图的代码后端文件。
/// 实现了画布上控件的鼠标拖拽移动、多选、键盘快捷键（复制/粘贴/撤销/重做/全选/删除）
/// 以及 Ctrl+滚轮缩放等交互逻辑。所有操作均通过 <see cref="DesignerViewModel"/> 驱动。
/// </summary>
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using HMIexe.App.ViewModels;
using HMIexe.Core.Models.Controls;
using HMIexe.Core.UndoRedo;

namespace HMIexe.App.Views;

/// <summary>
/// HMI 设计器视图，提供可视化控件拖放设计画布。
/// 通过隧道事件拦截画布上的鼠标和键盘输入，实现控件的选择、移动、缩放等操作，
/// 并将移动操作推送到撤销/重做历史记录中。
/// </summary>
public partial class DesignerView : UserControl
{
    /// <summary>当前是否正处于拖拽控件的状态。</summary>
    private bool _isDragging;

    /// <summary>鼠标按下时的起始坐标（相对于画布），用于计算拖拽偏移量。</summary>
    private Point _dragStartPoint;

    /// <summary>
    /// 当前拖拽操作涉及的控件列表，记录每个控件及其拖拽开始时的原始坐标，
    /// 以便在拖拽结束后计算实际偏移并提交到撤销历史。
    /// </summary>
    private List<(HmiControlBase Ctrl, double OrigX, double OrigY)> _dragItems = new();

    /// <summary>
    /// 初始化 <see cref="DesignerView"/> 的新实例，并注册画布的隧道指针事件和滚轮事件处理器。
    /// 使用隧道策略可在子控件消费事件之前优先拦截，确保设计器行为的优先级。
    /// </summary>
    public DesignerView()
    {
        InitializeComponent();

        // 使用隧道策略拦截指针事件，确保在子控件消费事件前优先处理
        DesignCanvas.AddHandler(PointerPressedEvent, OnCanvasPointerPressed, RoutingStrategies.Tunnel);
        DesignCanvas.AddHandler(PointerMovedEvent, OnCanvasPointerMoved, RoutingStrategies.Tunnel);
        DesignCanvas.AddHandler(PointerReleasedEvent, OnCanvasPointerReleased, RoutingStrategies.Tunnel);
        DesignCanvas.AddHandler(PointerWheelChangedEvent, OnCanvasPointerWheel, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// 获取当前视图绑定的 <see cref="DesignerViewModel"/>，若 DataContext 不匹配则返回 null。
    /// </summary>
    private DesignerViewModel? Vm => DataContext as DesignerViewModel;

    /// <summary>
    /// 处理画布鼠标按下事件，实现控件的点选和多选，并准备拖拽状态。
    /// 锁定的控件可以被选中但不会参与拖拽移动。
    /// </summary>
    /// <param name="sender">触发事件的对象（画布）。</param>
    /// <param name="e">鼠标按下事件参数，包含坐标和修饰键信息。</param>
    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var vm = Vm;
        if (vm?.CurrentPage == null) return;

        var pos = e.GetCurrentPoint(DesignCanvas).Position;
        // 按住 Ctrl（Windows/Linux）或 Meta（macOS）时启用多选模式
        bool multiSelect = e.KeyModifiers.HasFlag(KeyModifiers.Control) ||
                           e.KeyModifiers.HasFlag(KeyModifiers.Meta);

        // 对画布坐标执行命中测试，找到鼠标位置最上层的控件
        var hit = HitTestControl(vm, pos.X, pos.Y);

        if (hit != null && hit.Locked)
        {
            // 锁定的控件仅允许选中，不进行拖拽
            vm.SelectControl(hit, multiSelect);
            return;
        }

        vm.SelectControl(hit, multiSelect);

        if (hit != null)
        {
            // 记录拖拽起始点及所有选中控件的当前坐标，供后续拖拽计算使用
            _dragStartPoint = pos;
            _dragItems = vm.SelectedControls
                .Select(c => (c, c.X, c.Y))
                .ToList();
            _isDragging = false; // 首次移动超过阈值后才设为 true
            e.Pointer.Capture(DesignCanvas);
        }

        // 确保画布获取键盘焦点，以便响应键盘快捷键
        Focus();
        e.Handled = true;
    }

    /// <summary>
    /// 处理画布鼠标移动事件，实时更新控件位置以实现拖拽效果。
    /// 超过 3 像素的移动阈值后才正式开始拖拽，避免误操作。
    /// 若启用了网格对齐，将坐标对齐到最近的网格点。
    /// </summary>
    /// <param name="sender">触发事件的对象（画布）。</param>
    /// <param name="e">鼠标移动事件参数，包含当前坐标信息。</param>
    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        var pos = e.GetCurrentPoint(DesignCanvas).Position;
        var vm = Vm;
        if (vm != null)
        {
            // 实时更新 ViewModel 中的鼠标坐标，供状态栏等 UI 元素显示
            vm.CanvasMouseX = Math.Round(pos.X);
            vm.CanvasMouseY = Math.Round(pos.Y);
        }

        if (_dragItems.Count == 0) return;
        if (vm == null) return;

        var dx = pos.X - _dragStartPoint.X;
        var dy = pos.Y - _dragStartPoint.Y;

        // 仅在移动超过 3 像素阈值后才开始拖拽，防止误触
        if (!_isDragging && (Math.Abs(dx) < 3 && Math.Abs(dy) < 3)) return;
        _isDragging = true;

        foreach (var (ctrl, origX, origY) in _dragItems)
        {
            var newX = origX + dx;
            var newY = origY + dy;

            // 若启用网格对齐，将坐标吸附到最近的网格位置
            if (vm.SnapToGrid && vm.GridSize > 0)
            {
                newX = Math.Round(newX / vm.GridSize) * vm.GridSize;
                newY = Math.Round(newY / vm.GridSize) * vm.GridSize;
            }

            // 限制控件不超出画布左边界和上边界（最小坐标为 0）
            ctrl.X = Math.Max(0, newX);
            ctrl.Y = Math.Max(0, newY);
        }

        e.Handled = true;
    }

    /// <summary>
    /// 处理画布鼠标释放事件，结束拖拽并将移动操作提交到撤销/重做历史记录。
    /// 仅将实际发生了位置变化的控件包含在撤销操作中。
    /// </summary>
    /// <param name="sender">触发事件的对象（画布）。</param>
    /// <param name="e">鼠标释放事件参数。</param>
    private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDragging && _dragItems.Count > 0)
        {
            var vm = Vm;
            if (vm != null)
            {
                // 仅将坐标实际发生变化的控件包含在撤销操作中，过滤未移动的控件
                var moves = _dragItems
                    .Where(item => Math.Abs(item.Ctrl.X - item.OrigX) > 0.001 ||
                                   Math.Abs(item.Ctrl.Y - item.OrigY) > 0.001)
                    .Select(item => (item.Ctrl, item.OrigX, item.OrigY, item.Ctrl.X, item.Ctrl.Y))
                    .ToList();

                // 将批量移动操作推送到撤销历史
                if (moves.Count > 0)
                    vm.UndoRedo.Push(new MoveMultipleControlsAction(moves));
            }
        }

        // 重置拖拽状态并释放指针捕获
        _isDragging = false;
        _dragItems.Clear();
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    /// <summary>
    /// 处理键盘按下事件，响应常见的设计器快捷键：
    /// Delete/Backspace（删除）、Ctrl+C（复制）、Ctrl+V（粘贴）、
    /// Ctrl+Z（撤销）、Ctrl+Y（重做）、Ctrl+A（全选）。
    /// 同时兼容 macOS 的 Cmd 键（Meta）。
    /// </summary>
    /// <param name="sender">触发事件的对象。</param>
    /// <param name="e">键盘按下事件参数，包含按键和修饰键信息。</param>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        var vm = Vm;
        if (vm == null) return;

        // 兼容 Windows/Linux（Ctrl）和 macOS（Cmd/Meta）的控制键
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

    /// <summary>
    /// 选中当前页面上的所有控件。
    /// 先清空现有选择，再以多选模式依次选中所有控件。
    /// </summary>
    /// <param name="vm">设计器 ViewModel 实例。</param>
    private static void SelectAll(DesignerViewModel vm)
    {
        if (vm.CurrentPage == null) return;
        var all = vm.CurrentPage.AllControls.ToList();
        // 先清空选择，再逐一添加到多选集合
        vm.SelectControl(null, false);
        foreach (var c in all)
            vm.SelectControl(c, true);
    }

    /// <summary>
    /// 对指定画布坐标执行命中测试，返回该点位置最上层（ZIndex 最大）的可见控件。
    /// </summary>
    /// <param name="vm">设计器 ViewModel 实例，提供当前页面的控件集合。</param>
    /// <param name="x">命中测试点的 X 坐标（相对于画布）。</param>
    /// <param name="y">命中测试点的 Y 坐标（相对于画布）。</param>
    /// <returns>命中的最上层可见控件；若未命中任何控件则返回 null。</returns>
    private static HmiControlBase? HitTestControl(DesignerViewModel vm, double x, double y)
    {
        if (vm.CurrentPage == null) return null;
        // 返回 ZIndex 最大（最顶层）且包含该点的可见控件
        return vm.CurrentPage.AllControls
            .Where(c => c.Visible && x >= c.X && x <= c.X + c.Width && y >= c.Y && y <= c.Y + c.Height)
            .OrderByDescending(c => c.ZIndex)
            .FirstOrDefault();
    }

    /// <summary>
    /// 处理画布鼠标滚轮事件，实现 Ctrl+滚轮缩放画布功能。
    /// 向上滚动放大 10%，向下滚动缩小 10%，缩放范围限制在 10% 至 500% 之间。
    /// </summary>
    /// <param name="sender">触发事件的对象（画布）。</param>
    /// <param name="e">鼠标滚轮事件参数，包含滚动方向和修饰键信息。</param>
    private void OnCanvasPointerWheel(object? sender, PointerWheelEventArgs e)
    {
        var vm = Vm;
        if (vm == null) return;
        // 仅在按住 Ctrl 或 Meta（macOS Cmd）时响应缩放操作
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) ||
            e.KeyModifiers.HasFlag(KeyModifiers.Meta))
        {
            // 向上滚动放大，向下滚动缩小，每次变化 0.1 倍
            var zoomDelta = e.Delta.Y > 0 ? 0.1 : -0.1;
            // 缩放比例限制在 0.1（10%）到 5.0（500%）之间
            vm.ZoomLevel = Math.Clamp(vm.ZoomLevel + zoomDelta, 0.1, 5.0);
            e.Handled = true;
        }
    }
}
