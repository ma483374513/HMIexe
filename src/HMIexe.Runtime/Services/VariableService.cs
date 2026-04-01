using HMIexe.Core.Models.Variables;
using HMIexe.Core.Services;
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
            var parts = SplitCsvLine(lines[i]);
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
                QuoteCsvField(v.Name),
                QuoteCsvField(v.DisplayName),
                QuoteCsvField(v.Type.ToString()),
                QuoteCsvField(v.Group),
                QuoteCsvField(v.Description),
                QuoteCsvField(v.DefaultValue?.ToString() ?? string.Empty),
                QuoteCsvField(v.Unit)));
        }
        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
    }

    private static string QuoteCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
            return $"\"{field.Replace("\"", "\"\"")}\"";
        return field;
    }

    private static string[] SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var fieldBuilder = new System.Text.StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    fieldBuilder.Append('"');
                    i++;
                }
                else if (c == '"')
                    inQuotes = false;
                else
                    fieldBuilder.Append(c);
            }
            else if (c == '"')
                inQuotes = true;
            else if (c == ',')
            {
                fields.Add(fieldBuilder.ToString());
                fieldBuilder.Clear();
            }
            else
                fieldBuilder.Append(c);
        }
        fields.Add(fieldBuilder.ToString());
        return fields.ToArray();
    }

    private void OnVariableValueChanged(object? sender, VariableValueChangedEventArgs e)
    {
        if (sender is HmiVariable)
            VariableValueChanged?.Invoke(this, e);
    }

    public event EventHandler<VariableValueChangedEventArgs>? VariableValueChanged;
}
