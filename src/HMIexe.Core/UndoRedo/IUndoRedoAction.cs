namespace HMIexe.Core.UndoRedo;

public interface IUndoRedoAction
{
    string Description { get; }
    void Execute();
    void Undo();
}
