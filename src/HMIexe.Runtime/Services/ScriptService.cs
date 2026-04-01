using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using HMIexe.Core.Models.Script;
using HMIexe.Core.Services;
using System.Collections.Concurrent;

namespace HMIexe.Runtime.Services;

public class ScriptService : IScriptService
{
    private readonly ConcurrentDictionary<string, HmiScript> _scripts = new();
    private readonly ConcurrentDictionary<string, Script<object?>> _compiledScripts = new();
    private readonly List<Timer> _timerHandles = new();

    private static readonly ScriptOptions DefaultOptions = ScriptOptions.Default
        .AddReferences(
            typeof(object).Assembly,
            typeof(Console).Assembly,
            typeof(System.Linq.Enumerable).Assembly,
            typeof(System.Collections.Generic.List<>).Assembly)
        .AddImports("System", "System.Linq", "System.Collections.Generic", "System.Math");

    public async Task<ScriptExecutionResult> ExecuteScriptAsync(
        string code, IDictionary<string, object?>? context = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            return new ScriptExecutionResult(true, null, null, TimeSpan.Zero);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var options = DefaultOptions;
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
            return new ScriptExecutionResult(false, null,
                string.Join(Environment.NewLine, ex.Diagnostics.Select(d => d.ToString())),
                sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new ScriptExecutionResult(false, null, ex.Message, sw.Elapsed);
        }
    }

    public async Task<CompileResult> CompileScriptAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return new CompileResult(true, [], []);

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

    public void RegisterScript(HmiScript script)
    {
        _scripts[script.Id] = script;
    }

    public void UnregisterScript(string scriptId)
    {
        _scripts.TryRemove(scriptId, out _);
        _compiledScripts.TryRemove(scriptId, out _);
    }

    public Task StartTimedScriptsAsync()
    {
        foreach (var script in _scripts.Values
            .Where(s => s.IsEnabled &&
                (s.TriggerType == ScriptTriggerType.Timer || s.TriggerType == ScriptTriggerType.Loop)))
        {
            var intervalMs = script.TimerIntervalMs > 0 ? script.TimerIntervalMs : 1000;
            var timer = new Timer(async _ =>
            {
                await ExecuteScriptAsync(script.Code);
            }, null, 0, intervalMs);
            _timerHandles.Add(timer);
        }
        return Task.CompletedTask;
    }

    public Task StopTimedScriptsAsync()
    {
        foreach (var timer in _timerHandles)
            timer.Dispose();
        _timerHandles.Clear();
        return Task.CompletedTask;
    }
}

public class ScriptGlobals
{
    private readonly IDictionary<string, object?> _context;

    public ScriptGlobals(IDictionary<string, object?> context) => _context = context;

    public object? GetVar(string name) =>
        _context.TryGetValue(name, out var v) ? v : null;

    public void SetVar(string name, object? value) =>
        _context[name] = value;
}
