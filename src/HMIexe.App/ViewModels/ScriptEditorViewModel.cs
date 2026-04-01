using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HMIexe.App.Services;
using HMIexe.Core.Models.Script;
using HMIexe.Core.Services;

namespace HMIexe.App.ViewModels;

public partial class ScriptEditorViewModel : ObservableObject
{
    private readonly IScriptService _scriptService;
    private readonly IDialogService _dialogService;

    public ObservableCollection<HmiScript> Scripts { get; } = new();

    [ObservableProperty]
    private HmiScript? _selectedScript;

    [ObservableProperty]
    private string _compileOutput = string.Empty;

    [ObservableProperty]
    private string _runOutput = string.Empty;

    [ObservableProperty]
    private bool _isCompiling;

    [ObservableProperty]
    private bool _isRunning;

    public IReadOnlyList<string> TriggerTypes { get; } = Enum.GetNames<ScriptTriggerType>();

    public ScriptEditorViewModel(IScriptService scriptService, IDialogService dialogService)
    {
        _scriptService = scriptService;
        _dialogService = dialogService;
    }

    partial void OnSelectedScriptChanged(HmiScript? value)
    {
        CompileOutput = string.Empty;
        RunOutput = string.Empty;
    }

    [RelayCommand]
    private void AddScript()
    {
        var script = new HmiScript
        {
            Name = $"Script{Scripts.Count + 1}",
            Code = "// 在此编写C#脚本\n// 可用API: GetVar(name), SetVar(name, value), Log(message)\n",
            TriggerType = ScriptTriggerType.Manual,
            IsEnabled = true
        };
        Scripts.Add(script);
        _scriptService.RegisterScript(script);
        SelectedScript = script;
    }

    [RelayCommand]
    private void RemoveScript()
    {
        if (SelectedScript == null) return;
        _scriptService.UnregisterScript(SelectedScript.Id);
        Scripts.Remove(SelectedScript);
        SelectedScript = Scripts.FirstOrDefault();
    }

    [RelayCommand]
    private async Task CompileScript()
    {
        if (SelectedScript == null) return;
        IsCompiling = true;
        CompileOutput = "编译中...";
        try
        {
            var result = await _scriptService.CompileScriptAsync(SelectedScript.Code);
            if (result.Success)
            {
                CompileOutput = "✓ 编译成功";
                if (result.Warnings.Count > 0)
                    CompileOutput += "\n警告:\n" + string.Join("\n", result.Warnings.Select(w => "  ⚠ " + w));
            }
            else
            {
                CompileOutput = "✗ 编译失败:\n" + string.Join("\n", result.Errors.Select(e => "  ✗ " + e));
            }
        }
        catch (Exception ex)
        {
            CompileOutput = $"✗ 异常: {ex.Message}";
        }
        finally
        {
            IsCompiling = false;
        }
    }

    [RelayCommand]
    private async Task RunScript()
    {
        if (SelectedScript == null) return;
        IsRunning = true;
        RunOutput = "运行中...";
        try
        {
            var result = await _scriptService.ExecuteScriptAsync(SelectedScript.Code);
            if (result.Success)
            {
                var ret = result.ReturnValue != null ? $"\n返回值: {result.ReturnValue}" : string.Empty;
                RunOutput = $"✓ 执行成功 ({result.Duration.TotalMilliseconds:F1}ms){ret}";
            }
            else
            {
                RunOutput = $"✗ 执行失败: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            RunOutput = $"✗ 异常: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
        }
    }

    [RelayCommand]
    private async Task StartTimedScripts()
    {
        await _scriptService.StartTimedScriptsAsync();
        await _dialogService.ShowMessageAsync("脚本", "定时脚本已启动");
    }

    [RelayCommand]
    private async Task StopTimedScripts()
    {
        await _scriptService.StopTimedScriptsAsync();
        await _dialogService.ShowMessageAsync("脚本", "定时脚本已停止");
    }

    public void LoadFromProject(IEnumerable<HmiScript> scripts)
    {
        Scripts.Clear();
        foreach (var s in scripts)
        {
            Scripts.Add(s);
            _scriptService.RegisterScript(s);
        }
        SelectedScript = Scripts.FirstOrDefault();
    }
}
