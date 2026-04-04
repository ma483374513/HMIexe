using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// 撤销/重做历史记录管理器。
/// 使用两个栈（撤销栈和重做栈）维护操作历史，
/// 继承 ObservableObject 以支持 CanUndo/CanRedo 等属性的 UI 绑定。
/// </summary>
namespace HMIexe.Core.UndoRedo;

/// <summary>
/// HMI 设计器的撤销/重做历史记录管理器。
/// 提供操作的执行、撤销、重做和清空功能，以及相应的可绑定状态属性。
/// </summary>
public class UndoRedoHistory : ObservableObject
{
    // 撤销操作栈：存储已执行的操作，栈顶为最近一次操作
    private readonly Stack<IUndoRedoAction> _undoStack = new();
    // 重做操作栈：存储已撤销的操作，栈顶为最近一次撤销的操作
    private readonly Stack<IUndoRedoAction> _redoStack = new();

    /// <summary>撤销栈是否有可撤销的操作；为 false 时撤销按钮应禁用。</summary>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>重做栈是否有可重做的操作；为 false 时重做按钮应禁用。</summary>
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>撤销菜单显示的文本，包含下一步将撤销的操作描述。</summary>
    public string UndoDescription => _undoStack.Count > 0 ? $"撤销: {_undoStack.Peek().Description}" : "撤销";

    /// <summary>重做菜单显示的文本，包含下一步将重做的操作描述。</summary>
    public string RedoDescription => _redoStack.Count > 0 ? $"重做: {_redoStack.Peek().Description}" : "重做";

    /// <summary>
    /// 通知所有与撤销/重做状态相关的可绑定属性发生了变化。
    /// 在栈内容改变后调用，以刷新 UI 中的按钮状态和菜单文本。
    /// </summary>
    private void NotifyAll()
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        OnPropertyChanged(nameof(UndoDescription));
        OnPropertyChanged(nameof(RedoDescription));
    }

    /// <summary>Record an already-executed action for undo without re-executing it.</summary>
    public void Push(IUndoRedoAction action)
    {
        _undoStack.Push(action);
        // 新操作入栈后清空重做栈，因为历史分支已改变
        _redoStack.Clear();
        NotifyAll();
    }

    /// <summary>
    /// 执行一个操作并将其压入撤销栈；同时清空重做栈。
    /// </summary>
    /// <param name="action">要执行的操作。</param>
    public void Execute(IUndoRedoAction action)
    {
        action.Execute();
        _undoStack.Push(action);
        // 执行新操作后，重做历史失效，清空重做栈
        _redoStack.Clear();
        NotifyAll();
    }

    /// <summary>
    /// 撤销最近一次操作：从撤销栈弹出操作并调用其 Undo 方法，然后将其压入重做栈。
    /// 若撤销栈为空则不执行任何操作。
    /// </summary>
    public void Undo()
    {
        if (!CanUndo) return;
        var action = _undoStack.Pop();
        action.Undo();
        // 将撤销的操作压入重做栈，以支持后续重做
        _redoStack.Push(action);
        NotifyAll();
    }

    /// <summary>
    /// 重做最近一次撤销的操作：从重做栈弹出操作并调用其 Execute 方法，然后将其压回撤销栈。
    /// 若重做栈为空则不执行任何操作。
    /// </summary>
    public void Redo()
    {
        if (!CanRedo) return;
        var action = _redoStack.Pop();
        action.Execute();
        // 将重做的操作压回撤销栈
        _undoStack.Push(action);
        NotifyAll();
    }

    /// <summary>清空撤销和重做历史记录（如打开新项目时调用）。</summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        NotifyAll();
    }
}
