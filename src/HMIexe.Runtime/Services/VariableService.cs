using HMIexe.Core.Models.Variables;
using HMIexe.Core.Services;
using HMIexe.Runtime.Utilities;
using System.Collections.ObjectModel;
using System.Text;

namespace HMIexe.Runtime.Services;

public class VariableService : IVariableService
{
    private readonly ObservableCollection<HmiVariable> _variables = new();

    public IReadOnlyList<HmiVariable> Variables => _variables;

    public HmiVariable? GetVariable(string id) =>
        _variables.FirstOrDefault(v => v.Id == id);

    public HmiVariable? GetVariableByName(string name) =>
        _variables.FirstOrDefault(v => v.Name == name);

    public void AddVariable(HmiVariable variable)
    {
        variable.ValueChanged += OnVariableValueChanged;
        _variables.Add(variable);
    }

    public void RemoveVariable(string id)
    {
        var variable = GetVariable(id);
        if (variable != null)
        {
            variable.ValueChanged -= OnVariableValueChanged;
            _variables.Remove(variable);
        }
    }

    public void UpdateVariable(string id, object? value)
    {
        var variable = GetVariable(id);
        if (variable != null)
            variable.Value = value;
    }

    public async Task ImportFromCsvAsync(string filePath)
    {
        var lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8);
        for (int i = 1; i < lines.Length; i++)
        {
            var parts = CsvHelper.SplitLine(lines[i]);
            if (parts.Length < 3) continue;
            var variable = new HmiVariable
            {
                Name = parts[0].Trim(),
                DisplayName = parts.Length > 1 ? parts[1].Trim() : parts[0].Trim(),
                Type = Enum.TryParse<VariableType>(parts[2].Trim(), out var t) ? t : VariableType.String,
                Group = parts.Length > 3 ? parts[3].Trim() : string.Empty,
                Description = parts.Length > 4 ? parts[4].Trim() : string.Empty
            };
            AddVariable(variable);
        }
    }

    public async Task ExportToCsvAsync(string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Name,DisplayName,Type,Group,Description,DefaultValue,Unit");
        foreach (var v in _variables)
        {
            sb.AppendLine(string.Join(",",
                CsvHelper.QuoteField(v.Name),
                CsvHelper.QuoteField(v.DisplayName),
                CsvHelper.QuoteField(v.Type.ToString()),
                CsvHelper.QuoteField(v.Group),
                CsvHelper.QuoteField(v.Description),
                CsvHelper.QuoteField(v.DefaultValue?.ToString() ?? string.Empty),
                CsvHelper.QuoteField(v.Unit)));
        }
        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
    }

    private void OnVariableValueChanged(object? sender, VariableValueChangedEventArgs e)
    {
        if (sender is HmiVariable)
            VariableValueChanged?.Invoke(this, e);
    }

    public event EventHandler<VariableValueChangedEventArgs>? VariableValueChanged;
}
