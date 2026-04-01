using System;
using System.Collections.Generic;
using HMIexe.Core.Models.Canvas;
using HMIexe.Core.Models.Controls;

namespace HMIexe.Core.UndoRedo;

public class AddControlAction : IUndoRedoAction
{
    private readonly HmiLayer _layer;
    private readonly HmiControlBase _control;

    public string Description => $"添加控件 '{_control.Name}'";

    public AddControlAction(HmiLayer layer, HmiControlBase control)
    {
        _layer = layer;
        _control = control;
    }

    public void Execute() => _layer.Controls.Add(_control);
    public void Undo() => _layer.Controls.Remove(_control);
}

public class RemoveControlsAction : IUndoRedoAction
{
    private readonly List<(HmiLayer Layer, HmiControlBase Control)> _items;

    public string Description => $"删除 {_items.Count} 个控件";

    public RemoveControlsAction(IEnumerable<(HmiLayer, HmiControlBase)> items)
    {
        _items = new List<(HmiLayer, HmiControlBase)>(items);
    }

    public void Execute()
    {
        foreach (var (layer, control) in _items)
            layer.Controls.Remove(control);
    }

    public void Undo()
    {
        foreach (var (layer, control) in _items)
            layer.Controls.Add(control);
    }
}

public class MoveControlAction : IUndoRedoAction
{
    private readonly HmiControlBase _control;
    private readonly double _oldX, _oldY, _newX, _newY;

    public string Description => $"移动 '{_control.Name}'";

    public MoveControlAction(HmiControlBase control, double oldX, double oldY, double newX, double newY)
    {
        _control = control;
        _oldX = oldX; _oldY = oldY; _newX = newX; _newY = newY;
    }

    public void Execute() { _control.X = _newX; _control.Y = _newY; }
    public void Undo() { _control.X = _oldX; _control.Y = _oldY; }
}

public class SetPropertyAction : IUndoRedoAction
{
    private readonly Action _execute;
    private readonly Action _undo;
    private readonly string _description;

    public string Description => _description;

    public SetPropertyAction(string description, Action execute, Action undo)
    {
        _description = description;
        _execute = execute;
        _undo = undo;
    }

    public void Execute() => _execute();
    public void Undo() => _undo();
}
