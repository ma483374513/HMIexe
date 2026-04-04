using System;
using System.Collections.Generic;
using HMIexe.Core.Models.Canvas;
using HMIexe.Core.Models.Controls;

/// <summary>
/// 设计器撤销/重做操作具体实现集合。
/// 包含控件的添加、删除、移动（单个/批量）及通用属性变更等操作类，
/// 均实现 <see cref="IUndoRedoAction"/> 接口，可被 <see cref="UndoRedoHistory"/> 管理。
/// </summary>
namespace HMIexe.Core.UndoRedo;

/// <summary>
/// 向图层中添加控件的可撤销操作。
/// Execute 将控件加入图层，Undo 将其从图层中移除。
/// </summary>
public class AddControlAction : IUndoRedoAction
{
    // 目标图层引用
    private readonly HmiLayer _layer;
    // 要添加的控件引用
    private readonly HmiControlBase _control;

    /// <summary>操作描述，格式为 "添加控件 '控件名'"，显示在撤销/重做菜单中。</summary>
    public string Description => $"添加控件 '{_control.Name}'";

    /// <summary>
    /// 初始化添加控件操作。
    /// </summary>
    /// <param name="layer">目标图层。</param>
    /// <param name="control">要添加的控件。</param>
    public AddControlAction(HmiLayer layer, HmiControlBase control)
    {
        _layer = layer;
        _control = control;
    }

    /// <summary>执行：将控件添加到图层的控件列表末尾。</summary>
    public void Execute() => _layer.Controls.Add(_control);

    /// <summary>撤销：将控件从图层的控件列表中移除。</summary>
    public void Undo() => _layer.Controls.Remove(_control);
}

/// <summary>
/// 从图层中删除一个或多个控件的可撤销操作。
/// Execute 将所有控件从各自图层中移除，Undo 将其重新添加回原图层。
/// </summary>
public class RemoveControlsAction : IUndoRedoAction
{
    // 存储每个控件与其所属图层的配对列表
    private readonly List<(HmiLayer Layer, HmiControlBase Control)> _items;

    /// <summary>操作描述，格式为 "删除 N 个控件"。</summary>
    public string Description => $"删除 {_items.Count} 个控件";

    /// <summary>
    /// 初始化批量删除控件操作。
    /// </summary>
    /// <param name="items">要删除的控件与图层配对集合。</param>
    public RemoveControlsAction(IEnumerable<(HmiLayer, HmiControlBase)> items)
    {
        _items = new List<(HmiLayer, HmiControlBase)>(items);
    }

    /// <summary>执行：将所有控件从对应图层中移除。</summary>
    public void Execute()
    {
        foreach (var (layer, control) in _items)
            layer.Controls.Remove(control);
    }

    /// <summary>撤销：将所有控件重新添加回对应图层。</summary>
    public void Undo()
    {
        foreach (var (layer, control) in _items)
            layer.Controls.Add(control);
    }
}

/// <summary>
/// 移动单个控件位置的可撤销操作。
/// Execute 将控件移动到新坐标，Undo 将其恢复到原坐标。
/// </summary>
public class MoveControlAction : IUndoRedoAction
{
    // 要移动的控件引用
    private readonly HmiControlBase _control;
    // 移动前后的坐标
    private readonly double _oldX, _oldY, _newX, _newY;

    /// <summary>操作描述，格式为 "移动 '控件名'"。</summary>
    public string Description => $"移动 '{_control.Name}'";

    /// <summary>
    /// 初始化控件移动操作。
    /// </summary>
    /// <param name="control">要移动的控件。</param>
    /// <param name="oldX">移动前的 X 坐标。</param>
    /// <param name="oldY">移动前的 Y 坐标。</param>
    /// <param name="newX">移动后的 X 坐标。</param>
    /// <param name="newY">移动后的 Y 坐标。</param>
    public MoveControlAction(HmiControlBase control, double oldX, double oldY, double newX, double newY)
    {
        _control = control;
        _oldX = oldX; _oldY = oldY; _newX = newX; _newY = newY;
    }

    /// <summary>执行：将控件移动到新坐标位置。</summary>
    public void Execute() { _control.X = _newX; _control.Y = _newY; }

    /// <summary>撤销：将控件恢复到移动前的坐标位置。</summary>
    public void Undo() { _control.X = _oldX; _control.Y = _oldY; }
}

/// <summary>
/// 批量移动多个控件位置的可撤销操作。
/// Execute 将所有控件移动到各自的新坐标，Undo 将所有控件恢复到原坐标。
/// </summary>
public class MoveMultipleControlsAction : IUndoRedoAction
{
    // 存储每个控件移动前后坐标的列表
    private readonly List<(HmiControlBase Control, double OldX, double OldY, double NewX, double NewY)> _items;

    /// <summary>操作描述，格式为 "移动 N 个控件"。</summary>
    public string Description => $"移动 {_items.Count} 个控件";

    /// <summary>
    /// 初始化批量移动控件操作。
    /// </summary>
    /// <param name="items">包含每个控件及其移动前后坐标的集合。</param>
    public MoveMultipleControlsAction(IEnumerable<(HmiControlBase, double, double, double, double)> items)
    {
        _items = new List<(HmiControlBase, double, double, double, double)>(items);
    }

    /// <summary>执行：将所有控件移动到各自的新坐标。</summary>
    public void Execute()
    {
        foreach (var (ctrl, _, _, nx, ny) in _items) { ctrl.X = nx; ctrl.Y = ny; }
    }

    /// <summary>撤销：将所有控件恢复到各自的原坐标。</summary>
    public void Undo()
    {
        foreach (var (ctrl, ox, oy, _, _) in _items) { ctrl.X = ox; ctrl.Y = oy; }
    }
}

/// <summary>
/// 通用属性变更的可撤销操作。
/// 通过委托封装任意属性的 execute（设新值）和 undo（恢复旧值）逻辑，
/// 适用于不需要专用类的单次属性修改操作。
/// </summary>
public class SetPropertyAction : IUndoRedoAction
{
    // 执行操作的委托（设置新属性值）
    private readonly Action _execute;
    // 撤销操作的委托（恢复旧属性值）
    private readonly Action _undo;
    // 操作描述文本
    private readonly string _description;

    /// <summary>操作描述，由调用者在构造时提供。</summary>
    public string Description => _description;

    /// <summary>
    /// 初始化通用属性变更操作。
    /// </summary>
    /// <param name="description">操作的中文描述，显示在撤销/重做菜单中。</param>
    /// <param name="execute">执行操作的委托（设置新值）。</param>
    /// <param name="undo">撤销操作的委托（恢复旧值）。</param>
    public SetPropertyAction(string description, Action execute, Action undo)
    {
        _description = description;
        _execute = execute;
        _undo = undo;
    }

    /// <summary>执行：调用 execute 委托设置新属性值。</summary>
    public void Execute() => _execute();

    /// <summary>撤销：调用 undo 委托恢复旧属性值。</summary>
    public void Undo() => _undo();
}
