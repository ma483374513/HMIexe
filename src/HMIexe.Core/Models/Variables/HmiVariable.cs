using System.ComponentModel;
using System.Text.Json.Serialization;

/// <summary>
/// HMI 变量模型定义。
/// 包含 HmiVariable（带运行时值变更通知的变量对象）及 VariableValueChangedEventArgs（值变更事件参数）。
/// HMI 变量是数据绑定的核心，控件、脚本和通信通道均通过变量交换数据。
/// </summary>
namespace HMIexe.Core.Models.Variables;

/// <summary>
/// HMI 变量，表示工程中定义的一个数据点。
/// 变量可与控件属性、通信地址绑定，其值在运行时由通信驱动或脚本更新。
/// 实现 <see cref="INotifyPropertyChanged"/> 以支持 UI 数据绑定；
/// <see cref="Value"/> 属性不参与 JSON 序列化（仅为运行时状态）。
/// </summary>
public class HmiVariable : INotifyPropertyChanged
{
    // 私有字段：运行时当前值，不参与序列化
    private object? _value;

    /// <summary>变量的唯一标识符（GUID）。</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>变量名称，在脚本和绑定表达式中通过此名称引用变量，项目内应唯一。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>变量的显示名称，用于属性面板和报表展示，允许使用中文或友好名称。</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>变量数据类型，决定值的存储格式和合法输入范围，默认为整型（Int）。</summary>
    public VariableType Type { get; set; } = VariableType.Int;

    /// <summary>变量所属的分组名称，用于在变量管理器中组织和过滤变量。</summary>
    public string Group { get; set; } = string.Empty;

    /// <summary>变量的说明文字，记录该变量的含义和来源。</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>变量的默认初始值；项目启动时若无通信数据则使用此值。</summary>
    public object? DefaultValue { get; set; }

    /// <summary>变量的最小允许值；用于范围校验，超出范围时可触发报警。</summary>
    public object? MinValue { get; set; }

    /// <summary>变量的最大允许值；用于范围校验，超出范围时可触发报警。</summary>
    public object? MaxValue { get; set; }

    /// <summary>变量的工程单位（如 "℃"、"bar"），用于在界面和报表中显示单位标注。</summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>是否为只读变量；只读变量不允许通过界面操作写入，仅可由通信驱动更新。</summary>
    public bool IsReadOnly { get; set; }

    /// <summary>变量最后一次被修改的时间戳；每次 Value 变更时自动更新。</summary>
    public DateTime LastModified { get; set; } = DateTime.Now;

    /// <summary>
    /// 变量的当前运行时值（不参与 JSON 序列化）。
    /// 仅当新值与旧值不相等时才触发属性变更通知和 <see cref="ValueChanged"/> 事件，
    /// 并自动更新 <see cref="LastModified"/> 时间戳。
    /// </summary>
    [JsonIgnore]
    public object? Value
    {
        get => _value;
        set
        {
            // 使用 Equals 进行值比较，避免无意义的重复通知
            if (!Equals(_value, value))
            {
                var oldValue = _value;
                _value = value;
                // 记录最后修改时间
                LastModified = DateTime.Now;
                // 触发 INotifyPropertyChanged 通知，刷新绑定到此变量的 UI 控件
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                // 触发值变更事件，供通信服务和脚本引擎订阅
                ValueChanged?.Invoke(this, new VariableValueChangedEventArgs(Name, oldValue, value));
            }
        }
    }

    /// <summary>属性变更通知事件（INotifyPropertyChanged），用于 UI 数据绑定刷新。</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>变量值变更事件，携带旧值、新值及变量名，供通信服务和脚本引擎订阅处理。</summary>
    public event EventHandler<VariableValueChangedEventArgs>? ValueChanged;
}

/// <summary>
/// 变量值变更事件参数。
/// 在 <see cref="HmiVariable.ValueChanged"/> 事件触发时传递，包含变量名、旧值、新值和时间戳。
/// </summary>
public class VariableValueChangedEventArgs : EventArgs
{
    /// <summary>发生值变更的变量名称。</summary>
    public string VariableName { get; }

    /// <summary>变更前的旧值。</summary>
    public object? OldValue { get; }

    /// <summary>变更后的新值。</summary>
    public object? NewValue { get; }

    /// <summary>值变更发生的时间戳，默认为事件参数对象创建时的当前时间。</summary>
    public DateTime Timestamp { get; } = DateTime.Now;

    /// <summary>
    /// 初始化变量值变更事件参数。
    /// </summary>
    /// <param name="variableName">发生变更的变量名称。</param>
    /// <param name="oldValue">变更前的旧值。</param>
    /// <param name="newValue">变更后的新值。</param>
    public VariableValueChangedEventArgs(string variableName, object? oldValue, object? newValue)
    {
        VariableName = variableName;
        OldValue = oldValue;
        NewValue = newValue;
    }
}
