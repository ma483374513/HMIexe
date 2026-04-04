using HMIexe.Core.Models.Variables;
using HMIexe.Core.Services;
using HMIexe.Runtime.Utilities;
using System.Collections.ObjectModel;
using System.Text;

namespace HMIexe.Runtime.Services;

/// <summary>
/// 变量服务，实现 <see cref="IVariableService"/> 接口。
/// 维护运行时变量集合，提供变量的增删改查、CSV 批量导入/导出功能，
/// 并通过 <see cref="VariableValueChanged"/> 事件将变量值变化传播给上层订阅者。
/// </summary>
public class VariableService : IVariableService
{
    /// <summary>运行时变量集合，使用 <see cref="ObservableCollection{T}"/> 支持 UI 绑定。</summary>
    private readonly ObservableCollection<HmiVariable> _variables = new();

    /// <summary>获取当前所有变量的只读列表。</summary>
    public IReadOnlyList<HmiVariable> Variables => _variables;

    /// <summary>
    /// 根据变量 ID 查找变量。
    /// </summary>
    /// <param name="id">变量唯一标识符。</param>
    /// <returns>找到的变量实例，不存在时返回 <c>null</c>。</returns>
    public HmiVariable? GetVariable(string id) =>
        _variables.FirstOrDefault(v => v.Id == id);

    /// <summary>
    /// 根据变量名称查找变量。
    /// </summary>
    /// <param name="name">变量名称（区分大小写）。</param>
    /// <returns>找到的变量实例，不存在时返回 <c>null</c>。</returns>
    public HmiVariable? GetVariableByName(string name) =>
        _variables.FirstOrDefault(v => v.Name == name);

    /// <summary>
    /// 向服务中添加一个新变量，并订阅其 <see cref="HmiVariable.ValueChanged"/> 事件，
    /// 以便将变量级别的变化事件提升为服务级别的 <see cref="VariableValueChanged"/> 事件。
    /// </summary>
    /// <param name="variable">要添加的变量实例。</param>
    public void AddVariable(HmiVariable variable)
    {
        variable.ValueChanged += OnVariableValueChanged;
        _variables.Add(variable);
    }

    /// <summary>
    /// 从服务中移除指定 ID 的变量，并取消订阅其值变化事件。
    /// 若变量不存在，则忽略此调用。
    /// </summary>
    /// <param name="id">要移除的变量 ID。</param>
    public void RemoveVariable(string id)
    {
        var variable = GetVariable(id);
        if (variable != null)
        {
            variable.ValueChanged -= OnVariableValueChanged;
            _variables.Remove(variable);
        }
    }

    /// <summary>
    /// 更新指定 ID 变量的当前值，触发该变量的 <see cref="HmiVariable.ValueChanged"/> 事件。
    /// 若变量不存在，则忽略此调用。
    /// </summary>
    /// <param name="id">变量 ID。</param>
    /// <param name="value">新值。</param>
    public void UpdateVariable(string id, object? value)
    {
        var variable = GetVariable(id);
        if (variable != null)
            variable.Value = value;
    }

    /// <summary>
    /// 从 CSV 文件异步批量导入变量定义（UTF-8 编码）。
    /// CSV 格式（首行为表头，跳过）：Name, DisplayName, Type, Group, Description
    /// 类型字段使用 <see cref="VariableType"/> 枚举名，解析失败时默认为 <see cref="VariableType.String"/>。
    /// </summary>
    /// <param name="filePath">CSV 文件路径。</param>
    public async Task ImportFromCsvAsync(string filePath)
    {
        var lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8);
        // 从第二行（索引 1）开始，跳过表头行
        for (int i = 1; i < lines.Length; i++)
        {
            var parts = CsvHelper.SplitLine(lines[i]);
            if (parts.Length < 3) continue; // 字段不足时跳过该行
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

    /// <summary>
    /// 将当前所有变量定义异步导出为 CSV 文件（UTF-8 编码）。
    /// CSV 列顺序：Name、DisplayName、Type、Group、Description、DefaultValue、Unit。
    /// </summary>
    /// <param name="filePath">目标文件路径。</param>
    public async Task ExportToCsvAsync(string filePath)
    {
        var sb = new StringBuilder();
        // 写入表头
        sb.AppendLine("Name,DisplayName,Type,Group,Description,DefaultValue,Unit");
        foreach (var v in _variables)
        {
            // 每个字段通过 CsvHelper.QuoteField 转义，保证含特殊字符的值也能正确输出
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

    /// <summary>
    /// 处理单个变量的值变化事件，将其包装为服务级 <see cref="VariableValueChanged"/> 事件并广播。
    /// 使用变量名称作为标识，与变量 ID 无关，方便报警条件等上层逻辑订阅。
    /// </summary>
    private void OnVariableValueChanged(object? sender, VariableValueChangedEventArgs e)
    {
        if (sender is HmiVariable variable)
            VariableValueChanged?.Invoke(this, new VariableValueChangedEventArgs(variable.Name, e.OldValue, e.NewValue));
    }

    /// <summary>当任意变量的值发生变化时引发，携带变量名称、旧值和新值。</summary>
    public event EventHandler<VariableValueChangedEventArgs>? VariableValueChanged;
}
