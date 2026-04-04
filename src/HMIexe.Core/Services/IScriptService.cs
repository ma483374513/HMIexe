using HMIexe.Core.Models.Script;

/// <summary>
/// 脚本服务接口定义。
/// 提供脚本的编译、执行、注册/注销及定时脚本生命周期管理等能力，
/// 是 HMI 脚本引擎（基于 Roslyn C# 脚本）的核心契约。
/// </summary>
namespace HMIexe.Core.Services;

/// <summary>
/// HMI 脚本服务接口，负责脚本的编译验证和运行时执行调度。
/// 支持异步执行、上下文变量传递及定时脚本的启停管理。
/// </summary>
public interface IScriptService
{
    /// <summary>
    /// 异步执行一段脚本代码，并返回执行结果。
    /// </summary>
    /// <param name="code">要执行的 C# 脚本代码字符串。</param>
    /// <param name="context">传递给脚本的上下文变量字典（键为变量名，值为变量值）；为 null 时使用空上下文。</param>
    /// <returns>包含执行成功标志、返回值、错误消息和执行耗时的结果记录。</returns>
    Task<ScriptExecutionResult> ExecuteScriptAsync(string code, IDictionary<string, object?>? context = null);

    /// <summary>
    /// 异步编译（预检）一段脚本代码，返回编译错误和警告列表。
    /// 用于设计器中对脚本进行语法校验，不执行代码。
    /// </summary>
    /// <param name="code">要编译检查的 C# 脚本代码字符串。</param>
    /// <returns>包含编译成功标志、错误列表和警告列表的结果记录。</returns>
    Task<CompileResult> CompileScriptAsync(string code);

    /// <summary>
    /// 向脚本服务注册一个脚本定义，使其可被定时触发或事件触发系统管理。
    /// </summary>
    /// <param name="script">要注册的脚本定义对象。</param>
    void RegisterScript(HmiScript script);

    /// <summary>
    /// 从脚本服务中注销指定 ID 的脚本，停止其触发调度。
    /// </summary>
    /// <param name="scriptId">要注销的脚本 ID。</param>
    void UnregisterScript(string scriptId);

    /// <summary>异步启动所有已注册的定时脚本（TriggerType 为 Timer 或 Loop）的调度器。</summary>
    Task StartTimedScriptsAsync();

    /// <summary>异步停止所有正在运行的定时脚本调度器。</summary>
    Task StopTimedScriptsAsync();
}

/// <summary>
/// 脚本执行结果记录（record）。
/// </summary>
/// <param name="Success">脚本是否成功执行完毕（无运行时异常）。</param>
/// <param name="ReturnValue">脚本最后一条表达式的返回值；无返回值时为 null。</param>
/// <param name="ErrorMessage">执行失败时的错误消息；成功时为 null。</param>
/// <param name="Duration">脚本执行耗时。</param>
public record ScriptExecutionResult(bool Success, object? ReturnValue, string? ErrorMessage, TimeSpan Duration);

/// <summary>
/// 脚本编译结果记录（record）。
/// </summary>
/// <param name="Success">脚本是否编译通过（无错误）。</param>
/// <param name="Errors">编译错误列表；编译成功时为空列表。</param>
/// <param name="Warnings">编译警告列表；可为空列表。</param>
public record CompileResult(bool Success, IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings);
