using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HMIexe.App.Services;
using HMIexe.Core.Models.Variables;
using HMIexe.Core.Services;

namespace HMIexe.App.ViewModels;

public partial class VariableManagerViewModel : ObservableObject
{
    private readonly IVariableService _variableService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private HmiVariable? _selectedVariable;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ObservableCollection<HmiVariable> Variables { get; } = new();
    public ObservableCollection<HmiVariable> FilteredVariables { get; } = new();
    public ObservableCollection<string> Groups { get; } = new();

    public VariableManagerViewModel(IVariableService variableService, IDialogService dialogService)
    {
        _variableService = variableService;
        _dialogService = dialogService;

        foreach (var v in _variableService.Variables)
            Variables.Add(v);

        RefreshFilter();
    }

    partial void OnSearchTextChanged(string value) => RefreshFilter();

    private void RefreshFilter()
    {
        FilteredVariables.Clear();
        var term = SearchText?.Trim() ?? string.Empty;
        foreach (var v in Variables)
        {
            if (string.IsNullOrEmpty(term) ||
                v.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                v.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                v.Group.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                FilteredVariables.Add(v);
            }
        }
    }

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
        RefreshFilter();
        SelectedVariable = variable;
    }

    [RelayCommand]
    private void RemoveVariable()
    {
        if (SelectedVariable == null) return;
        _variableService.RemoveVariable(SelectedVariable.Id);
        Variables.Remove(SelectedVariable);
        FilteredVariables.Remove(SelectedVariable);
        SelectedVariable = null;
    }

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
            RefreshFilter();
            await _dialogService.ShowMessageAsync("导入成功", $"已导入 {_variableService.Variables.Count} 个变量");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("导入失败", ex.Message);
        }
    }

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
