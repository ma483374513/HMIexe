using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HMIexe.App.Services;
using HMIexe.Core.Models.Alarm;
using HMIexe.Core.Services;
using HMIexe.Runtime.Services;

namespace HMIexe.App.ViewModels;

public partial class AlarmManagerViewModel : ObservableObject
{
    private readonly IAlarmService _alarmService;
    private readonly IDialogService _dialogService;
    private readonly AlarmConditionEvaluator _conditionEvaluator;

    public ObservableCollection<HmiAlarmDefinition> AlarmDefinitions { get; } = new();
    public ObservableCollection<HmiAlarmRecord> ActiveAlarms { get; } = new();
    public ObservableCollection<HmiAlarmRecord> AlarmHistory { get; } = new();

    [ObservableProperty]
    private HmiAlarmDefinition? _selectedDefinition;

    [ObservableProperty]
    private HmiAlarmRecord? _selectedActiveAlarm;

    [ObservableProperty]
    private int _selectedTabIndex;

    public IReadOnlyList<AlarmSeverity> SeverityLevels { get; } = Enum.GetValues<AlarmSeverity>();

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

    private void OnAlarmRaised(object? sender, HmiAlarmRecord record)
    {
        ActiveAlarms.Add(record);
        AlarmHistory.Insert(0, record);
    }

    private void OnAlarmAcknowledged(object? sender, HmiAlarmRecord record)
    {
        var active = ActiveAlarms.FirstOrDefault(a => a.Id == record.Id);
        if (active != null)
            ActiveAlarms.Remove(active);
    }

    private void RefreshAlarms()
    {
        ActiveAlarms.Clear();
        foreach (var a in _alarmService.ActiveAlarms)
            ActiveAlarms.Add(a);

        AlarmHistory.Clear();
        foreach (var h in _alarmService.AlarmHistory.OrderByDescending(h => h.OccurredAt))
            AlarmHistory.Add(h);
    }

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

    [RelayCommand]
    private void RemoveDefinition()
    {
        if (SelectedDefinition == null) return;
        AlarmDefinitions.Remove(SelectedDefinition);
        SelectedDefinition = AlarmDefinitions.FirstOrDefault();
        _conditionEvaluator.SetDefinitions(AlarmDefinitions);
    }

    [RelayCommand]
    private void TestRaiseAlarm()
    {
        if (SelectedDefinition == null) return;
        _alarmService.RaiseAlarm(SelectedDefinition);
    }

    [RelayCommand]
    private async Task AcknowledgeSelectedAlarm()
    {
        if (SelectedActiveAlarm == null) return;
        var user = await _dialogService.ShowInputAsync("确认报警", "操作员名称:", "操作员");
        if (string.IsNullOrEmpty(user)) return;
        _alarmService.AcknowledgeAlarm(SelectedActiveAlarm.Id, user);
        RefreshAlarms();
    }

    [RelayCommand]
    private void AcknowledgeAllAlarms()
    {
        foreach (var alarm in ActiveAlarms.ToList())
            _alarmService.AcknowledgeAlarm(alarm.Id, "系统");
        RefreshAlarms();
    }

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

    [RelayCommand]
    private void RefreshView() => RefreshAlarms();

    public void LoadFromProject(IEnumerable<HmiAlarmDefinition> definitions)
    {
        AlarmDefinitions.Clear();
        foreach (var d in definitions)
            AlarmDefinitions.Add(d);
        SelectedDefinition = AlarmDefinitions.FirstOrDefault();
        _conditionEvaluator.SetDefinitions(AlarmDefinitions);
    }
}
