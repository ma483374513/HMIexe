using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using HMIexe.Core.Models.Script;
using HMIexe.Core.Services;
using System.Collections.Concurrent;

namespace HMIexe.Runtime.Services;

/// <summary>
/// 脚本服务，实现 <see cref="IScriptService"/> 接口。
/// 基于 Roslyn（Microsoft.CodeAnalysis）提供运行时 C# 脚本的执行和编译能力，
/// 支持按需执行、编译校验、脚本注册/注销，以及定时/循环脚本的启停管理。
/// </summary>
public class ScriptService : IScriptService
{
    /// <summary>已注册的脚本字典，键为脚本 ID。</summary>
    private readonly ConcurrentDictionary<string, HmiScript> _scripts = new();

    /// <summary>预编译脚本的缓存字典，键为脚本 ID，用于提升重复执行的性能。</summary>
    private readonly ConcurrentDictionary<string, Script<object?>> _compiledScripts = new();

    /// <summary>定时脚本对应的 <see cref="Timer"/> 句柄列表，用于统一停止。</summary>
    private readonly List<Timer> _timerHandles = new();

    /// <summary>
    /// 默认脚本执行选项：引用常用程序集并导入常用命名空间，
    /// 使脚本中可直接使用 System、LINQ、Math 等 API。
    /// </summary>
    private static readonly ScriptOptions DefaultOptions = ScriptOptions.Default
        .AddReferences(
            typeof(object).Assembly,
            typeof(Console).Assembly,
            typeof(System.Linq.Enumerable).Assembly,
            typeof(System.Collections.Generic.List<>).Assembly)
        .AddImports("System", "System.Linq", "System.Collections.Generic", "System.Math");

    /// <summary>
    /// 异步执行一段 C# 脚本代码，可选传入上下文变量字典供脚本访问。
    /// 捕获编译错误和运行时异常，均以失败结果返回而不抛出异常。
    /// </summary>
    /// <param name="code">要执行的 C# 代码字符串。</param>
    /// <param name="context">可选的上下文变量字典，脚本中可通过 <c>GetVar</c>/<c>SetVar</c> 访问。</param>
    /// <returns>
    /// <see cref="ScriptExecutionResult"/>，包含执行是否成功、返回值、错误信息和耗时。
    /// </returns>
    public async Task<ScriptExecutionResult> ExecuteScriptAsync(
        string code, IDictionary<string, object?>? context = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            return new ScriptExecutionResult(true, null, null, TimeSpan.Zero);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var options = DefaultOptions;
            // 若提供了上下文，则通过 ScriptGlobals 传入，使脚本可访问变量
            var globals = context != null ? new ScriptGlobals(context) : null;
            var result = globals != null
                ? await CSharpScript.EvaluateAsync<object?>(code, options, globals, typeof(ScriptGlobals))
                : await CSharpScript.EvaluateAsync<object?>(code, options);
            sw.Stop();
            return new ScriptExecutionResult(true, result, null, sw.Elapsed);
        }
        catch (CompilationErrorException ex)
        {
            sw.Stop();
            // 编译失败：将所有诊断信息拼接为错误消息
            return new ScriptExecutionResult(false, null,
                string.Join(Environment.NewLine, ex.Diagnostics.Select(d => d.ToString())),
                sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            // 运行时异常：返回异常消息
            return new ScriptExecutionResult(false, null, ex.Message, sw.Elapsed);
        }
    }

    /// <summary>
    /// 异步编译指定代码并返回编译结果（不执行），
    /// 可用于脚本编辑器中的语法校验功能。
    /// </summary>
    /// <param name="code">要编译的 C# 代码字符串。</param>
    /// <returns>
    /// <see cref="CompileResult"/>，包含是否编译成功、错误列表和警告列表。
    /// </returns>
    public async Task<CompileResult> CompileScriptAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return new CompileResult(true, [], []);

        // 使用 Roslyn 创建内存编译单元进行诊断分析
        var compilation = CSharpCompilation.Create(
            "HmiScript",
            new[] { CSharpSyntaxTree.ParseText(code) },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location)
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var diagnostics = compilation.GetDiagnostics();
        // 分别收集错误和警告
        var errors = diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.ToString())
            .ToList();
        var warnings = diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Warning)
            .Select(d => d.ToString())
            .ToList();

        return await Task.FromResult(new CompileResult(errors.Count == 0, errors, warnings));
    }

    /// <summary>
    /// 注册一个脚本定义，使其可被服务管理（执行、定时触发等）。
    /// 若已存在相同 ID 的脚本，则替换。
    /// </summary>
    /// <param name="script">要注册的脚本对象。</param>
    public void RegisterScript(HmiScript script)
    {
        _scripts[script.Id] = script;
    }

    /// <summary>
    /// 注销指定 ID 的脚本，同时清除对应的预编译缓存。
    /// </summary>
    /// <param name="scriptId">要注销的脚本 ID。</param>
    public void UnregisterScript(string scriptId)
    {
        _scripts.TryRemove(scriptId, out _);
        _compiledScripts.TryRemove(scriptId, out _);
    }

    /// <summary>
    /// 启动所有已注册的定时/循环脚本（<see cref="ScriptTriggerType.Timer"/> 或 <see cref="ScriptTriggerType.Loop"/>）。
    /// 每个脚本创建一个独立的 <see cref="Timer"/>，间隔由 <see cref="HmiScript.TimerIntervalMs"/> 决定，
    /// 默认为 1000 毫秒。脚本执行异常仅写入标准错误，不影响其他脚本。
    /// </summary>
    public Task StartTimedScriptsAsync()
    {
        foreach (var script in _scripts.Values
            .Where(s => s.IsEnabled &&
                (s.TriggerType == ScriptTriggerType.Timer || s.TriggerType == ScriptTriggerType.Loop)))
        {
            // 间隔至少 1 秒，避免无效值导致过于频繁的执行
            var intervalMs = script.TimerIntervalMs > 0 ? script.TimerIntervalMs : 1000;
            var timer = new Timer(async _ =>
            {
                try
                {
                    await ExecuteScriptAsync(script.Code);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[ScriptService] Timed script '{script.Name}' failed: {ex.Message}");
                }
            }, null, 0, intervalMs);
            _timerHandles.Add(timer);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 停止所有定时脚本：释放全部 <see cref="Timer"/> 实例并清空句柄列表。
    /// </summary>
    public Task StopTimedScriptsAsync()
    {
        foreach (var timer in _timerHandles)
            timer.Dispose();
        _timerHandles.Clear();
        return Task.CompletedTask;
    }
}

/// <summary>
/// 脚本执行全局上下文，作为 Roslyn 脚本的 globals 对象传入。
/// 使脚本代码可通过 <see cref="GetVar"/> 和 <see cref="SetVar"/> 读写外部变量字典。
/// </summary>
public class ScriptGlobals
{
    /// <summary>上下文变量字典，键为变量名，值为任意对象。</summary>
    private readonly IDictionary<string, object?> _context;

    /// <summary>
    /// 使用指定上下文字典初始化 <see cref="ScriptGlobals"/>。
    /// </summary>
    /// <param name="context">外部传入的变量上下文。</param>
    public ScriptGlobals(IDictionary<string, object?> context) => _context = context;

    /// <summary>
    /// 从上下文中读取指定名称的变量值。
    /// 若变量不存在，则返回 <c>null</c>。
    /// </summary>
    /// <param name="name">变量名称。</param>
    /// <returns>变量值，不存在时为 <c>null</c>。</returns>
    public object? GetVar(string name) =>
        _context.TryGetValue(name, out var v) ? v : null;

    /// <summary>
    /// 向上下文中写入或更新指定名称的变量值。
    /// </summary>
    /// <param name="name">变量名称。</param>
    /// <param name="value">要设置的值。</param>
    public void SetVar(string name, object? value) =>
        _context[name] = value;
}
