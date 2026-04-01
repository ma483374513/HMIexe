using HMIexe.Core.Models.Project;

namespace HMIexe.Core.Services;

public interface IProjectService
{
    HmiProject? CurrentProject { get; }
    bool IsDirty { get; }
    Task<HmiProject> CreateNewProjectAsync(string name);
    Task<HmiProject> OpenProjectAsync(string filePath);
    Task SaveProjectAsync();
    Task SaveProjectAsAsync(string filePath);
    Task<string> ExportProjectAsync(string outputPath);
    Task<HmiProject> ImportProjectAsync(string packagePath);
    IReadOnlyList<string> RecentProjects { get; }
    event EventHandler<HmiProject?> ProjectChanged;
}
