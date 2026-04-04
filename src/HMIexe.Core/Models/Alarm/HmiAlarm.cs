/// <summary>
/// 报警模型定义。
/// 包含报警严重级别枚举、报警定义（触发条件与配置）以及运行时报警记录（历史与状态跟踪）。
/// </summary>
namespace HMIexe.Core.Models.Alarm;

/// <summary>
/// 报警严重级别枚举。
/// 表示报警的严重程度，从低到高依次为：信息、警告、错误、严重。
/// </summary>
public enum AlarmSeverity
{
    /// <summary>信息级别，仅作提示使用。</summary>
    Info,
    /// <summary>警告级别，需要关注但不影响正常运行。</summary>
    Warning,
    /// <summary>错误级别，影响部分功能，需要处理。</summary>
    Error,
    /// <summary>严重级别，系统或设备出现严重故障。</summary>
    Critical
}

/// <summary>
/// HMI 报警定义。
/// 描述一条报警规则的配置信息，包括触发条件、严重级别、提示消息及确认脚本。
/// </summary>
public class HmiAlarmDefinition
{
    /// <summary>报警定义的唯一标识符（GUID）。</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>报警名称，用于在界面中显示和识别该报警规则。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>报警触发条件表达式，运行时引擎将对此表达式求值以判断是否触发报警。</summary>
    public string Condition { get; set; } = string.Empty;

    /// <summary>报警的严重级别，默认为警告（Warning）。</summary>
    public AlarmSeverity Severity { get; set; } = AlarmSeverity.Warning;

    /// <summary>报警触发时显示给操作员的提示消息。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>是否启用该报警规则；禁用后运行时将跳过条件检测。</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>操作员确认报警时执行的脚本代码。</summary>
    public string AcknowledgeScript { get; set; } = string.Empty;
}

/// <summary>
/// HMI 报警记录。
/// 表示运行时产生的一条报警实例，记录发生时间、确认状态及操作人信息。
/// </summary>
public class HmiAlarmRecord
{
    /// <summary>报警记录的唯一标识符（GUID）。</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>关联的报警定义 ID，用于追溯该记录由哪条规则触发。</summary>
    public string AlarmDefinitionId { get; set; } = string.Empty;

    /// <summary>报警发生时记录的提示消息（可能含动态变量值）。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>该报警记录的严重级别，继承自对应的报警定义。</summary>
    public AlarmSeverity Severity { get; set; }

    /// <summary>报警发生的时间戳，默认为记录创建时的当前时间。</summary>
    public DateTime OccurredAt { get; set; } = DateTime.Now;

    /// <summary>操作员确认报警的时间戳；未确认时为 null。</summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>确认报警的操作员名称或标识；未确认时为空字符串。</summary>
    public string AcknowledgedBy { get; set; } = string.Empty;

    /// <summary>报警是否仍处于激活状态；恢复正常后应将此属性设置为 false。</summary>
    public bool IsActive { get; set; } = true;
}
