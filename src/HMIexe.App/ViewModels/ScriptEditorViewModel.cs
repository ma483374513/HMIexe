/// <summary>
/// 脚本编辑器视图模型文件。
/// 负责 C# 脚本的增删管理、编译验证、手动执行以及定时脚本的启停控制。
/// </summary>
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HMIexe.App.Services;
using HMIexe.Core.Models.Script;
using HMIexe.Core.Services;

namespace HMIexe.App.ViewModels;

/// <summary>
/// 脚本编辑器视图模型。
/// 提供 HMI 工程脚本的完整生命周期管理，包括代码编辑、语法编译验证、
/// 即时运行调试和定时触发脚本的启停控制。
/// </summary>
public partial class ScriptEditorViewModel : ObservableObject
{
    /// <summary>脚本业务服务，提供脚本的注册、编译和执行能力。</summary>
    private readonly IScriptService _scriptService;

    /// <summary>对话框服务，用于显示操作结果和错误提示。</summary>
    private readonly IDialogService _dialogService;

    /// <summary>当前工程的所有脚本集合，绑定到脚本列表视图。</summary>
    public ObservableCollection<HmiScript> Scripts { get; } = new();

    /// <summary>当前在脚本列表中选中的脚本对象。</summary>
    [ObservableProperty]
    private HmiScript? _selectedScript;

    /// <summary>编译操作的输出文本，包含成功提示、警告列表或错误详情。</summary>
    [ObservableProperty]
    private string _compileOutput = string.Empty;

    /// <summary>运行操作的输出文本，包含执行耗时、返回值或错误信息。</summary>
    [ObservableProperty]
    private string _runOutput = string.Empty;

    /// <summary>指示编译操作是否正在进行中，用于控制 UI 的等待状态。</summary>
    [ObservableProperty]
    private bool _isCompiling;

    /// <summary>指示脚本运行是否正在进行中，用于控制 UI 的等待状态。</summary>
    [ObservableProperty]
    private bool _isRunning;

    /// <summary>所有支持的脚本触发类型枚举值列表，绑定到触发类型下拉框。</summary>
    public IReadOnlyList<ScriptTriggerType> TriggerTypes { get; } = Enum.GetValues<ScriptTriggerType>();

    /// <summary>
    /// 初始化脚本编辑器视图模型。
    /// </summary>
    /// <param name="scriptService">脚本业务服务。</param>
    /// <param name="dialogService">UI 对话框服务。</param>
    public ScriptEditorViewModel(IScriptService scriptService, IDialogService dialogService)
    {
        _scriptService = scriptService;
        _dialogService = dialogService;
    }

    /// <summary>
    /// 选中脚本变更时清空编译和运行的输出文本，避免显示上一个脚本的结果。
    /// </summary>
    /// <param name="value">新选中的脚本对象。</param>
    partial void OnSelectedScriptChanged(HmiScript? value)
    {
        CompileOutput = string.Empty;
        RunOutput = string.Empty;
    }

    /// <summary>
    /// 添加新脚本命令。创建包含示例注释的默认 C# 脚本并注册到服务。
    /// </summary>
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

    /// <summary>
    /// 删除选中脚本命令。从服务注销脚本并从集合中移除。
    /// </summary>
    [RelayCommand]
    private void RemoveScript()
    {
        if (SelectedScript == null) return;
        _scriptService.UnregisterScript(SelectedScript.Id);
        Scripts.Remove(SelectedScript);
        SelectedScript = Scripts.FirstOrDefault();
    }

    /// <summary>
    /// 编译当前选中脚本命令。
    /// 将脚本代码提交给服务进行 Roslyn 编译，并将编译成功/失败信息、
    /// 警告列表格式化后显示在 <see cref="CompileOutput"/> 中。
    /// </summary>
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
                // 若编译通过但有警告，将所有警告逐行追加到输出
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

    /// <summary>
    /// 运行当前选中脚本命令。
    /// 执行脚本代码并将运行耗时、返回值或错误信息格式化后显示在 <see cref="RunOutput"/> 中。
    /// </summary>
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
                // 若脚本有返回值则追加显示
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

    /// <summary>
    /// 启动所有定时触发脚本命令，使配置了周期触发器的脚本开始按计划执行。
    /// </summary>
    [RelayCommand]
    private async Task StartTimedScripts()
    {
        await _scriptService.StartTimedScriptsAsync();
        await _dialogService.ShowMessageAsync("脚本", "定时脚本已启动");
    }

    /// <summary>
    /// 停止所有定时触发脚本命令，取消所有正在运行的周期计划。
    /// </summary>
    [RelayCommand]
    private async Task StopTimedScripts()
    {
        await _scriptService.StopTimedScriptsAsync();
        await _dialogService.ShowMessageAsync("脚本", "定时脚本已停止");
    }

    /// <summary>
    /// 从工程数据加载脚本集合，并将每个脚本注册到脚本服务。
    /// </summary>
    /// <param name="scripts">工程中保存的脚本集合。</param>
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
