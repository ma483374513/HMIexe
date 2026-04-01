using System.Text.Json;
using HMIexe.Core.Models.Project;
using HMIexe.Core.Services;

namespace HMIexe.Runtime.Services;

public class ProjectService : IProjectService
{
    private readonly List<string> _recentProjects = new();
    private string? _currentFilePath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public HmiProject? CurrentProject { get; private set; }
    public bool IsDirty { get; private set; }
    public IReadOnlyList<string> RecentProjects => _recentProjects;

    public event EventHandler<HmiProject?>? ProjectChanged;

    public async Task<HmiProject> CreateNewProjectAsync(string name)
    {
        var project = new HmiProject { Name = name };
        var defaultPage = new Core.Models.Canvas.HmiPage
        {
            Name = "主画面",
            IsDefault = true
        };
        defaultPage.Layers.Add(new Core.Models.Canvas.HmiLayer { Name = "图层1" });
        project.Pages.Add(defaultPage);
        project.DefaultPageId = defaultPage.Id;

        CurrentProject = project;
        IsDirty = false;
        _currentFilePath = null;
        ProjectChanged?.Invoke(this, project);
        return await Task.FromResult(project);
    }

    public async Task<HmiProject> OpenProjectAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        var project = JsonSerializer.Deserialize<HmiProject>(json, JsonOptions)
            ?? throw new InvalidDataException("Invalid project file.");

        CurrentProject = project;
        IsDirty = false;
        _currentFilePath = filePath;

        if (!_recentProjects.Contains(filePath))
        {
            _recentProjects.Insert(0, filePath);
            if (_recentProjects.Count > 10) _recentProjects.RemoveAt(10);
        }

        ProjectChanged?.Invoke(this, project);
        return project;
    }

    public async Task SaveProjectAsync()
    {
        if (CurrentProject == null) return;
        if (_currentFilePath == null)
        {
            throw new InvalidOperationException("No file path set. Use SaveProjectAsAsync.");
        }
        await SaveToFileAsync(_currentFilePath);
    }

    public async Task SaveProjectAsAsync(string filePath)
    {
        if (CurrentProject == null) return;
        await SaveToFileAsync(filePath);
        _currentFilePath = filePath;
        if (!_recentProjects.Contains(filePath))
        {
            _recentProjects.Insert(0, filePath);
            if (_recentProjects.Count > 10) _recentProjects.RemoveAt(10);
        }
    }

    private async Task SaveToFileAsync(string filePath)
    {
        if (CurrentProject == null) return;
        CurrentProject.ModifiedAt = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(CurrentProject, JsonOptions);
        var dir = Path.GetDirectoryName(filePath);
        if (dir != null) Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(filePath, json);
        IsDirty = false;
    }

    public async Task<string> ExportProjectAsync(string outputPath)
    {
        if (CurrentProject == null) throw new InvalidOperationException("No project open.");
        var json = JsonSerializer.Serialize(CurrentProject, JsonOptions);
        var packagePath = Path.Combine(outputPath, $"{CurrentProject.Name}_{DateTime.UtcNow:yyyyMMddHHmmss}.hmiproj");
        await File.WriteAllTextAsync(packagePath, json);
        return packagePath;
    }

    public async Task<HmiProject> ImportProjectAsync(string packagePath)
    {
        return await OpenProjectAsync(packagePath);
    }
}
