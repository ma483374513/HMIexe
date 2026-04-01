using HMIexe.Core.Models.Script;

namespace HMIexe.Core.Services;

public interface IScriptService
{
    Task<ScriptExecutionResult> ExecuteScriptAsync(string code, IDictionary<string, object?>? context = null);
    Task<CompileResult> CompileScriptAsync(string code);
    void RegisterScript(HmiScript script);
    void UnregisterScript(string scriptId);
    Task StartTimedScriptsAsync();
    Task StopTimedScriptsAsync();
}

public record ScriptExecutionResult(bool Success, object? ReturnValue, string? ErrorMessage, TimeSpan Duration);
public record CompileResult(bool Success, IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings);
