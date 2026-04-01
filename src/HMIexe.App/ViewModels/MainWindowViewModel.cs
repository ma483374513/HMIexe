using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HMIexe.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "HMI Designer";

    [ObservableProperty]
    private object? _currentContent;

    [ObservableProperty]
    private bool _isProjectOpen;

    public ObservableCollection<string> RecentProjects { get; } = new();

    [RelayCommand]
    private void NewProject()
    {
        // TODO: Open new project dialog
    }

    [RelayCommand]
    private void OpenProject()
    {
        // TODO: Open file dialog
    }

    [RelayCommand]
    private void SaveProject()
    {
        // TODO: Save current project
    }
}
