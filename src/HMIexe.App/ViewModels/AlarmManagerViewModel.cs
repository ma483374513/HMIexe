/// <summary>
/// 报警管理器视图模型文件。
/// 负责报警定义的增删管理、活动报警的确认操作，以及报警历史的查看与导出。
/// </summary>
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HMIexe.App.Services;
using HMIexe.Core.Models.Alarm;
using HMIexe.Core.Services;
using HMIexe.Runtime.Services;

namespace HMIexe.App.ViewModels;

/// <summary>
/// 报警管理器视图模型。
/// 提供报警定义配置、活动报警监控（含一键/单条确认）、历史记录浏览和 CSV 导出等功能。
/// 通过订阅 <see cref="IAlarmService"/> 的事件实时更新 UI 列表。
/// </summary>
public partial class AlarmManagerViewModel : ObservableObject
{
    /// <summary>报警业务服务，负责报警的触发、确认和历史记录管理。</summary>
    private readonly IAlarmService _alarmService;

    /// <summary>对话框服务，用于显示输入框和消息提示。</summary>
    private readonly IDialogService _dialogService;

    /// <summary>报警条件求值器，在定义变更时同步更新求值规则。</summary>
    private readonly AlarmConditionEvaluator _conditionEvaluator;

    /// <summary>当前已配置的所有报警定义集合，绑定到定义列表视图。</summary>
    public ObservableCollection<HmiAlarmDefinition> AlarmDefinitions { get; } = new();

    /// <summary>当前正在触发（未确认或已确认但未复位）的活动报警集合。</summary>
    public ObservableCollection<HmiAlarmRecord> ActiveAlarms { get; } = new();

    /// <summary>全部历史报警记录集合，按发生时间倒序排列。</summary>
    public ObservableCollection<HmiAlarmRecord> AlarmHistory { get; } = new();

    /// <summary>当前在定义列表中选中的报警定义。</summary>
    [ObservableProperty]
    private HmiAlarmDefinition? _selectedDefinition;

    /// <summary>当前在活动报警列表中选中的报警记录。</summary>
    [ObservableProperty]
    private HmiAlarmRecord? _selectedActiveAlarm;

    /// <summary>当前选中的标签页索引（0=定义、1=活动报警、2=历史记录）。</summary>
    [ObservableProperty]
    private int _selectedTabIndex;

    /// <summary>所有可用的报警严重级别枚举值列表，绑定到严重级别下拉框。</summary>
    public IReadOnlyList<AlarmSeverity> SeverityLevels { get; } = Enum.GetValues<AlarmSeverity>();

    /// <summary>
    /// 初始化报警管理器视图模型。
    /// 订阅报警触发与确认事件，并从服务加载初始数据。
    /// </summary>
    /// <param name="alarmService">报警业务服务。</param>
    /// <param name="dialogService">UI 对话框服务。</param>
    /// <param name="conditionEvaluator">报警条件求值器。</param>
    public AlarmManagerViewModel(IAlarmService alarmService, IDialogService dialogService,
        AlarmConditionEvaluator conditionEvaluator)
    {
        _alarmService = alarmService;
        _dialogService = dialogService;
        _conditionEvaluator = conditionEvaluator;

        _alarmService.AlarmRaised += OnAlarmRaised;
        _alarmService.AlarmAcknowledged += OnAlarmAcknowledged;

        RefreshAlarms();
    }

    /// <summary>
    /// 报警触发事件处理器。将新触发的报警同时添加到活动列表和历史列表头部。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="record">新触发的报警记录。</param>
    private void OnAlarmRaised(object? sender, HmiAlarmRecord record)
    {
        ActiveAlarms.Add(record);
        AlarmHistory.Insert(0, record);
    }

    /// <summary>
    /// 报警确认事件处理器。将已确认的报警从活动列表中移除。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="record">已确认的报警记录。</param>
    private void OnAlarmAcknowledged(object? sender, HmiAlarmRecord record)
    {
        var active = ActiveAlarms.FirstOrDefault(a => a.Id == record.Id);
        if (active != null)
            ActiveAlarms.Remove(active);
    }

    /// <summary>
    /// 从报警服务重新加载活动报警和历史记录，刷新 UI 列表。
    /// 历史记录按发生时间倒序排列。
    /// </summary>
    private void RefreshAlarms()
    {
        ActiveAlarms.Clear();
        foreach (var a in _alarmService.ActiveAlarms)
            ActiveAlarms.Add(a);

        AlarmHistory.Clear();
        foreach (var h in _alarmService.AlarmHistory.OrderByDescending(h => h.OccurredAt))
            AlarmHistory.Add(h);
    }

    /// <summary>
    /// 添加一条新的报警定义，并自动选中它，同时更新求值器规则。
    /// </summary>
    [RelayCommand]
    private void AddDefinition()
    {
        var def = new HmiAlarmDefinition
        {
            Name = $"Alarm{AlarmDefinitions.Count + 1}",
            Message = "报警消息",
            Severity = AlarmSeverity.Warning,
            IsEnabled = true
        };
        AlarmDefinitions.Add(def);
        SelectedDefinition = def;
        _conditionEvaluator.SetDefinitions(AlarmDefinitions);
    }

    /// <summary>
    /// 删除当前选中的报警定义，并更新求值器规则。
    /// </summary>
    [RelayCommand]
    private void RemoveDefinition()
    {
        if (SelectedDefinition == null) return;
        AlarmDefinitions.Remove(SelectedDefinition);
        SelectedDefinition = AlarmDefinitions.FirstOrDefault();
        _conditionEvaluator.SetDefinitions(AlarmDefinitions);
    }

    /// <summary>
    /// 手动触发当前选中报警定义对应的报警，用于测试目的。
    /// </summary>
    [RelayCommand]
    private void TestRaiseAlarm()
    {
        if (SelectedDefinition == null) return;
        _alarmService.RaiseAlarm(SelectedDefinition);
    }

    /// <summary>
    /// 确认当前选中的活动报警。弹出输入框要求操作员输入名称后执行确认。
    /// </summary>
    [RelayCommand]
    private async Task AcknowledgeSelectedAlarm()
    {
        if (SelectedActiveAlarm == null) return;
        var user = await _dialogService.ShowInputAsync("确认报警", "操作员名称:", "操作员");
        if (string.IsNullOrEmpty(user)) return;
        _alarmService.AcknowledgeAlarm(SelectedActiveAlarm.Id, user);
        RefreshAlarms();
    }

    /// <summary>
    /// 一键确认所有活动报警，操作员标记为"系统"。
    /// </summary>
    [RelayCommand]
    private void AcknowledgeAllAlarms()
    {
        foreach (var alarm in ActiveAlarms.ToList())
            _alarmService.AcknowledgeAlarm(alarm.Id, "系统");
        RefreshAlarms();
    }

    /// <summary>
    /// 将报警历史记录导出为 CSV 文件。弹出保存对话框让用户选择保存路径。
    /// </summary>
    [RelayCommand]
    private async Task ExportHistory()
    {
        var path = await _dialogService.SaveFileAsync("导出报警历史",
            [new FileFilter("CSV文件", ["csv"])], "alarm_history");
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            await _alarmService.ExportAlarmHistoryAsync(path);
            await _dialogService.ShowMessageAsync("导出", "报警历史已导出");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("错误", $"导出失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 手动刷新报警列表（活动报警和历史记录）。
    /// </summary>
    [RelayCommand]
    private void RefreshView() => RefreshAlarms();

    /// <summary>
    /// 从工程数据加载报警定义列表，替换当前内容并更新求值器。
    /// </summary>
    /// <param name="definitions">工程中保存的报警定义集合。</param>
    public void LoadFromProject(IEnumerable<HmiAlarmDefinition> definitions)
    {
        AlarmDefinitions.Clear();
        foreach (var d in definitions)
            AlarmDefinitions.Add(d);
        SelectedDefinition = AlarmDefinitions.FirstOrDefault();
        _conditionEvaluator.SetDefinitions(AlarmDefinitions);
    }
}
