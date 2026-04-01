using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HMIexe.Core.Models.Variables;

namespace HMIexe.App.ViewModels;

public partial class VariableManagerViewModel : ObservableObject
{
    [ObservableProperty]
    private HmiVariable? _selectedVariable;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ObservableCollection<HmiVariable> Variables { get; } = new();
    public ObservableCollection<string> Groups { get; } = new();

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
        Variables.Add(variable);
        SelectedVariable = variable;
    }

    [RelayCommand]
    private void RemoveVariable()
    {
        if (SelectedVariable != null)
        {
            Variables.Remove(SelectedVariable);
            SelectedVariable = null;
        }
    }

    [RelayCommand]
    private async Task ImportFromCsvAsync()
    {
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ExportToCsvAsync()
    {
        await Task.CompletedTask;
    }
}
