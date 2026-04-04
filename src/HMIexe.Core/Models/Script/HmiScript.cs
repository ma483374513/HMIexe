/// <summary>
/// HMI 脚本模型定义。
/// 包含脚本触发类型枚举和脚本元数据，支持手动触发、定时执行、
/// 变量变化触发及控件事件触发等多种触发方式。
/// </summary>
namespace HMIexe.Core.Models.Script;

/// <summary>
/// 脚本触发类型枚举，定义脚本何时被系统自动执行。
/// </summary>
public enum ScriptTriggerType
{
    /// <summary>手动触发，脚本仅在被显式调用时执行（如按钮点击脚本调用）。</summary>
    Manual,
    /// <summary>定时触发，按 <see cref="HmiScript.TimerIntervalMs"/> 指定的间隔执行一次后停止。</summary>
    Timer,
    /// <summary>循环触发，按固定间隔持续重复执行脚本。</summary>
    Loop,
    /// <summary>变量变化触发，当指定变量的值发生改变时执行脚本。</summary>
    VariableChange,
    /// <summary>页面加载触发，当页面导航完成并加载时执行脚本。</summary>
    PageLoad,
    /// <summary>页面关闭触发，当页面被关闭或离开时执行脚本。</summary>
    PageClose,
    /// <summary>控件事件触发，由指定控件的特定交互事件（如点击、值变化）触发执行。</summary>
    ControlEvent
}

/// <summary>
/// HMI 脚本定义，描述一段可在运行时执行的脚本代码及其触发配置。
/// 脚本使用 C# 脚本语言（基于 Roslyn），可访问 HMI 变量和系统 API。
/// </summary>
public class HmiScript
{
    /// <summary>脚本的唯一标识符（GUID）。</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>脚本名称，在脚本管理器中显示，默认为 "Script"。</summary>
    public string Name { get; set; } = "Script";

    /// <summary>脚本的实际代码内容（C# 脚本语法）。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>脚本的触发类型，决定脚本在何种条件下执行，默认为手动触发。</summary>
    public ScriptTriggerType TriggerType { get; set; } = ScriptTriggerType.Manual;

    /// <summary>
    /// 定时或循环触发的时间间隔（毫秒），默认为 1000ms（1 秒）。
    /// 仅当 TriggerType 为 Timer 或 Loop 时有效。
    /// </summary>
    public int TimerIntervalMs { get; set; } = 1000;

    /// <summary>
    /// 触发该脚本的变量 ID。
    /// 仅当 TriggerType 为 VariableChange 时有效。
    /// </summary>
    public string TriggerVariableId { get; set; } = string.Empty;

    /// <summary>
    /// 触发该脚本的控件 ID。
    /// 仅当 TriggerType 为 ControlEvent 时有效。
    /// </summary>
    public string TriggerControlId { get; set; } = string.Empty;

    /// <summary>
    /// 触发该脚本的控件事件名称（如 "Click"、"ValueChanged"）。
    /// 仅当 TriggerType 为 ControlEvent 时有效。
    /// </summary>
    public string TriggerEvent { get; set; } = string.Empty;

    /// <summary>是否启用该脚本；禁用后运行时将跳过此脚本的触发检测。</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>脚本的说明文字，用于记录脚本的功能和使用注意事项。</summary>
    public string Description { get; set; } = string.Empty;
}
