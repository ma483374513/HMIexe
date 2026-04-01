using HMIexe.Core.Models.Variables;

namespace HMIexe.Core.Services;

public interface IVariableService
{
    IReadOnlyList<HmiVariable> Variables { get; }
    HmiVariable? GetVariable(string id);
    HmiVariable? GetVariableByName(string name);
    void AddVariable(HmiVariable variable);
    void RemoveVariable(string id);
    void UpdateVariable(string id, object? value);
    Task ImportFromCsvAsync(string filePath);
    Task ExportToCsvAsync(string filePath);
    event EventHandler<VariableValueChangedEventArgs> VariableValueChanged;
}
