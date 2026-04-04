/// <summary>
/// 撤销/重做操作接口。
/// 所有可撤销的设计器操作均需实现此接口，以统一接入 <see cref="UndoRedoHistory"/> 的管理机制。
/// </summary>
namespace HMIexe.Core.UndoRedo;

/// <summary>
/// 可撤销/重做操作的抽象契约接口。
/// 实现类须提供操作描述、执行逻辑和撤销逻辑。
/// </summary>
public interface IUndoRedoAction
{
    /// <summary>操作的人类可读描述文本（中文），显示在撤销/重做菜单或工具提示中。</summary>
    string Description { get; }

    /// <summary>执行该操作（正向执行或重做时调用）。</summary>
    void Execute();

    /// <summary>撤销该操作，将系统状态恢复到执行前的状态。</summary>
    void Undo();
}
