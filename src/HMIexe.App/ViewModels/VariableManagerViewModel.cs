/// <summary>
/// 变量管理器视图模型文件。
/// 负责 HMI 工程变量的增删管理、分组筛选、关键字搜索，以及 CSV 格式的批量导入导出。
/// </summary>
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HMIexe.App.Services;
using HMIexe.Core.Models.Variables;
using HMIexe.Core.Services;

namespace HMIexe.App.ViewModels;

/// <summary>
/// 变量管理器视图模型。
/// 提供工程变量的全生命周期管理，支持按分组和关键字双重过滤，
/// 以及通过 CSV 文件批量导入和导出变量配置。
/// </summary>
public partial class VariableManagerViewModel : ObservableObject
{
    /// <summary>变量业务服务，提供变量的 CRUD 操作和 CSV 导入导出能力。</summary>
    private readonly IVariableService _variableService;

    /// <summary>对话框服务，用于文件选择、操作结果提示和错误显示。</summary>
    private readonly IDialogService _dialogService;

    /// <summary>当前在变量列表中选中的变量对象。</summary>
    [ObservableProperty]
    private HmiVariable? _selectedVariable;

    /// <summary>搜索关键字，变更时自动触发筛选刷新（匹配变量名、显示名或分组名）。</summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>当前选中的分组过滤条件；"全部"时不按分组过滤。</summary>
    [ObservableProperty]
    private string _selectedGroup = "全部";

    /// <summary>所有变量的完整集合，作为筛选的原始数据源。</summary>
    public ObservableCollection<HmiVariable> Variables { get; } = new();

    /// <summary>经过分组和关键字双重筛选后的变量集合，绑定到变量列表视图。</summary>
    public ObservableCollection<HmiVariable> FilteredVariables { get; } = new();

    /// <summary>可用分组名称列表，包含"全部"和所有已有分组，绑定到分组下拉框。</summary>
    public ObservableCollection<string> Groups { get; } = new();

    /// <summary>
    /// 初始化变量管理器视图模型。
    /// 从服务加载已有变量，并初始化分组列表和筛选结果。
    /// </summary>
    /// <param name="variableService">变量业务服务。</param>
    /// <param name="dialogService">UI 对话框服务。</param>
    public VariableManagerViewModel(IVariableService variableService, IDialogService dialogService)
    {
        _variableService = variableService;
        _dialogService = dialogService;

        foreach (var v in _variableService.Variables)
            Variables.Add(v);

        RefreshGroups();
        RefreshFilter();
    }

    /// <summary>搜索文本变更时自动刷新筛选列表。</summary>
    partial void OnSearchTextChanged(string value) => RefreshFilter();

    /// <summary>选中分组变更时自动刷新筛选列表。</summary>
    partial void OnSelectedGroupChanged(string value) => RefreshFilter();

    /// <summary>
    /// 重新构建分组列表。
    /// 从变量集合中提取所有非空分组名，去重排序后插入列表，
    /// 并尽量保持当前选中分组不变。
    /// </summary>
    private void RefreshGroups()
    {
        var current = SelectedGroup;
        Groups.Clear();
        Groups.Add("全部");
        var groups = Variables
            .Select(v => v.Group)
            .Where(g => !string.IsNullOrEmpty(g))
            .Distinct()
            .OrderBy(g => g)
            .ToList();
        foreach (var g in groups)
            Groups.Add(g);
        // 若原选中分组仍存在则保留，否则重置为"全部"
        SelectedGroup = Groups.Contains(current) ? current : "全部";
    }

    /// <summary>
    /// 根据当前分组和搜索条件重新填充 <see cref="FilteredVariables"/> 集合。
    /// 搜索同时匹配变量名、显示名和分组名（不区分大小写）。
    /// </summary>
    private void RefreshFilter()
    {
        FilteredVariables.Clear();
        var term = SearchText?.Trim() ?? string.Empty;
        foreach (var v in Variables)
        {
            var matchesGroup = SelectedGroup == "全部" || v.Group == SelectedGroup;
            var matchesSearch = string.IsNullOrEmpty(term) ||
                v.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                v.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                v.Group.Contains(term, StringComparison.OrdinalIgnoreCase);
            if (matchesGroup && matchesSearch)
                FilteredVariables.Add(v);
        }
    }

    /// <summary>
    /// 添加变量命令。创建一个默认整型变量并注册到服务，自动选中新变量。
    /// </summary>
    [RelayCommand]
    private void AddVariable()
    {
        var variable = new HmiVariable
        {
            Name = $"Var{Variables.Count + 1}",
            DisplayName = $"Variable {Variables.Count + 1}",
            Type = VariableType.Int,
            Value = 0
        };
        _variableService.AddVariable(variable);
        Variables.Add(variable);
        RefreshGroups();
        RefreshFilter();
        SelectedVariable = variable;
    }

    /// <summary>
    /// 删除变量命令。从服务和集合中移除当前选中的变量。
    /// </summary>
    [RelayCommand]
    private void RemoveVariable()
    {
        if (SelectedVariable == null) return;
        _variableService.RemoveVariable(SelectedVariable.Id);
        Variables.Remove(SelectedVariable);
        RefreshGroups();
        RefreshFilter();
        SelectedVariable = FilteredVariables.FirstOrDefault();
    }

    /// <summary>
    /// 从 CSV 文件批量导入变量命令。
    /// 弹出文件选择对话框后调用服务解析 CSV，成功后刷新列表并显示导入数量。
    /// </summary>
    [RelayCommand]
    private async Task ImportFromCsvAsync()
    {
        var path = await _dialogService.OpenFileAsync("导入变量",
            [new FileFilter("CSV文件", ["csv"])]);
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            await _variableService.ImportFromCsvAsync(path);
            Variables.Clear();
            foreach (var v in _variableService.Variables)
                Variables.Add(v);
            RefreshGroups();
            RefreshFilter();
            await _dialogService.ShowMessageAsync("导入成功", $"已导入 {_variableService.Variables.Count} 个变量");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("导入失败", ex.Message);
        }
    }

    /// <summary>
    /// 将当前变量列表导出为 CSV 文件命令。
    /// 弹出文件保存对话框后调用服务写入文件，成功后显示保存路径。
    /// </summary>
    [RelayCommand]
    private async Task ExportToCsvAsync()
    {
        var path = await _dialogService.SaveFileAsync("导出变量",
            [new FileFilter("CSV文件", ["csv"])], "variables");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            await _variableService.ExportToCsvAsync(path);
            await _dialogService.ShowMessageAsync("导出成功", $"已导出至：{path}");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("导出失败", ex.Message);
        }
    }
}
