using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HMIexe.Core.UndoRedo;

public class UndoRedoHistory : ObservableObject
{
    private readonly Stack<IUndoRedoAction> _undoStack = new();
    private readonly Stack<IUndoRedoAction> _redoStack = new();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    public string UndoDescription => _undoStack.Count > 0 ? $"撤销: {_undoStack.Peek().Description}" : "撤销";
    public string RedoDescription => _redoStack.Count > 0 ? $"重做: {_redoStack.Peek().Description}" : "重做";

    private void NotifyAll()
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        OnPropertyChanged(nameof(UndoDescription));
        OnPropertyChanged(nameof(RedoDescription));
    }

    public void Execute(IUndoRedoAction action)
    {
        action.Execute();
        _undoStack.Push(action);
        _redoStack.Clear();
        NotifyAll();
    }

    public void Undo()
    {
        if (!CanUndo) return;
        var action = _undoStack.Pop();
        action.Undo();
        _redoStack.Push(action);
        NotifyAll();
    }

    public void Redo()
    {
        if (!CanRedo) return;
        var action = _redoStack.Pop();
        action.Execute();
        _undoStack.Push(action);
        NotifyAll();
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        NotifyAll();
    }
}
