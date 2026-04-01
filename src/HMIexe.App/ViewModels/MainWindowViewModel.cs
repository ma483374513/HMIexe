using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HMIexe.App.Services;
using HMIexe.Core.Services;

namespace HMIexe.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IProjectService _projectService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private string _title = "HMI Designer";

    [ObservableProperty]
    private object? _currentContent;

    [ObservableProperty]
    private bool _isProjectOpen;

    [ObservableProperty]
    private string _statusText = "就绪";

    [ObservableProperty]
    private DesignerViewModel? _designerViewModel;

    public ObservableCollection<string> RecentProjects { get; } = new();

    public MainWindowViewModel(IProjectService projectService, IDialogService dialogService,
        DesignerViewModel designerViewModel)
    {
        _projectService = projectService;
        _dialogService = dialogService;
        DesignerViewModel = designerViewModel;
        CurrentContent = designerViewModel;

        _projectService.ProjectChanged += OnProjectChanged;

        foreach (var recent in _projectService.RecentProjects)
            RecentProjects.Add(recent);
    }

    private void OnProjectChanged(object? sender, Core.Models.Project.HmiProject? project)
    {
        if (project != null)
        {
            Title = $"HMI Designer - {project.Name}";
            IsProjectOpen = true;
            DesignerViewModel?.LoadProject(project);
        }
        else
        {
            Title = "HMI Designer";
            IsProjectOpen = false;
        }
    }

    [RelayCommand]
    private async Task NewProject()
    {
        if (_projectService.IsDirty)
        {
            var save = await _dialogService.ShowConfirmAsync("保存", "当前工程已修改，是否保存？");
            if (save) await SaveProject();
        }

        var name = await _dialogService.ShowInputAsync("新建工程", "请输入工程名称：", "新工程");
        if (string.IsNullOrWhiteSpace(name)) return;

        await _projectService.CreateNewProjectAsync(name);
        StatusText = $"已创建工程：{name}";
    }

    [RelayCommand]
    private async Task OpenProject()
    {
        if (_projectService.IsDirty)
        {
            var save = await _dialogService.ShowConfirmAsync("保存", "当前工程已修改，是否保存？");
            if (save) await SaveProject();
        }

        var path = await _dialogService.OpenFileAsync("打开工程",
            [new FileFilter("HMI工程文件", ["hmiproj", "json"])]);
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            await _projectService.OpenProjectAsync(path);
            StatusText = $"已打开：{path}";
            RecentProjects.Clear();
            foreach (var r in _projectService.RecentProjects)
                RecentProjects.Add(r);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("错误", $"打开工程失败：{ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SaveProject()
    {
        if (_projectService.CurrentProject == null) return;

        try
        {
            await _projectService.SaveProjectAsync();
            StatusText = "保存成功";
        }
        catch (InvalidOperationException)
        {
            await SaveProjectAs();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("错误", $"保存失败：{ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SaveProjectAs()
    {
        if (_projectService.CurrentProject == null) return;

        var path = await _dialogService.SaveFileAsync("另存为",
            [new FileFilter("HMI工程文件", ["hmiproj"])],
            _projectService.CurrentProject.Name);
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            await _projectService.SaveProjectAsAsync(path);
            StatusText = $"已保存至：{path}";
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("错误", $"保存失败：{ex.Message}");
        }
    }
}
