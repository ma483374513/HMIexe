using System.Text.Json;
using HMIexe.Core.Models.Project;
using HMIexe.Core.Services;

namespace HMIexe.Runtime.Services;

/// <summary>
/// 项目服务，实现 <see cref="IProjectService"/> 接口。
/// 负责 HMI 项目的完整生命周期管理：新建、打开、保存、另存为、导出和导入。
/// 项目数据以 JSON 格式持久化，同时维护最近打开记录（最多 10 条）。
/// </summary>
public class ProjectService : IProjectService
{
    /// <summary>最近打开的项目文件路径列表，最多保留 10 条。</summary>
    private readonly List<string> _recentProjects = new();

    /// <summary>当前项目文件的保存路径；新建项目时为 <c>null</c>。</summary>
    private string? _currentFilePath;

    /// <summary>
    /// JSON 序列化选项：输出格式化缩进，且反序列化时属性名不区分大小写。
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>获取当前加载的 HMI 项目；若未加载任何项目则为 <c>null</c>。</summary>
    public HmiProject? CurrentProject { get; private set; }

    /// <summary>指示当前项目自上次保存后是否有未保存的更改。</summary>
    public bool IsDirty { get; private set; }

    /// <summary>获取最近打开的项目路径只读列表。</summary>
    public IReadOnlyList<string> RecentProjects => _recentProjects;

    /// <summary>当项目被创建、打开或关闭时引发，携带新的项目对象（关闭时为 <c>null</c>）。</summary>
    public event EventHandler<HmiProject?>? ProjectChanged;

    /// <summary>
    /// 异步创建一个新的 HMI 项目，包含一个默认画面和一个默认图层。
    /// 新项目不绑定文件路径，<see cref="IsDirty"/> 重置为 <c>false</c>。
    /// </summary>
    /// <param name="name">项目名称。</param>
    /// <returns>新创建的 <see cref="HmiProject"/> 实例。</returns>
    public async Task<HmiProject> CreateNewProjectAsync(string name)
    {
        var project = new HmiProject { Name = name };
        // 创建默认画面并添加默认图层
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

    /// <summary>
    /// 异步从指定文件路径加载项目（JSON 格式），并更新最近打开记录。
    /// </summary>
    /// <param name="filePath">项目文件的完整路径。</param>
    /// <returns>反序列化得到的 <see cref="HmiProject"/> 实例。</returns>
    /// <exception cref="InvalidDataException">文件内容无效或无法反序列化时抛出。</exception>
    public async Task<HmiProject> OpenProjectAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        var project = JsonSerializer.Deserialize<HmiProject>(json, JsonOptions)
            ?? throw new InvalidDataException("Invalid project file.");

        CurrentProject = project;
        IsDirty = false;
        _currentFilePath = filePath;

        // 将路径插入最近列表头部，超出 10 条时移除最旧的记录
        if (!_recentProjects.Contains(filePath))
        {
            _recentProjects.Insert(0, filePath);
            if (_recentProjects.Count > 10) _recentProjects.RemoveAt(10);
        }

        ProjectChanged?.Invoke(this, project);
        return project;
    }

    /// <summary>
    /// 将当前项目保存到原文件路径。
    /// 若尚未设置文件路径（新建项目），则抛出异常，应改用 <see cref="SaveProjectAsAsync"/>。
    /// </summary>
    /// <exception cref="InvalidOperationException">未设置文件路径时抛出。</exception>
    public async Task SaveProjectAsync()
    {
        if (CurrentProject == null) return;
        if (_currentFilePath == null)
        {
            throw new InvalidOperationException("No file path set. Use SaveProjectAsAsync.");
        }
        await SaveToFileAsync(_currentFilePath);
    }

    /// <summary>
    /// 将当前项目另存为指定路径，并更新当前文件路径和最近打开记录。
    /// </summary>
    /// <param name="filePath">新的目标文件路径。</param>
    public async Task SaveProjectAsAsync(string filePath)
    {
        if (CurrentProject == null) return;
        await SaveToFileAsync(filePath);
        _currentFilePath = filePath;
        // 更新最近打开记录
        if (!_recentProjects.Contains(filePath))
        {
            _recentProjects.Insert(0, filePath);
            if (_recentProjects.Count > 10) _recentProjects.RemoveAt(10);
        }
    }

    /// <summary>
    /// 将当前项目序列化为 JSON 并写入文件，同时更新 <see cref="HmiProject.ModifiedAt"/> 时间戳。
    /// 写入完成后将 <see cref="IsDirty"/> 重置为 <c>false</c>。
    /// </summary>
    /// <param name="filePath">目标文件路径。</param>
    private async Task SaveToFileAsync(string filePath)
    {
        if (CurrentProject == null) return;
        CurrentProject.ModifiedAt = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(CurrentProject, JsonOptions);
        // 确保目标目录存在
        var dir = Path.GetDirectoryName(filePath);
        if (dir != null) Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(filePath, json);
        IsDirty = false;
    }

    /// <summary>
    /// 将当前项目导出为独立的 <c>.hmiproj</c> 文件（实质为 JSON），
    /// 文件名包含项目名称和导出时间戳。
    /// </summary>
    /// <param name="outputPath">导出目标目录。</param>
    /// <returns>导出文件的完整路径。</returns>
    /// <exception cref="InvalidOperationException">当前无打开的项目时抛出。</exception>
    public async Task<string> ExportProjectAsync(string outputPath)
    {
        if (CurrentProject == null) throw new InvalidOperationException("No project open.");
        var json = JsonSerializer.Serialize(CurrentProject, JsonOptions);
        var packagePath = Path.Combine(outputPath, $"{CurrentProject.Name}_{DateTime.UtcNow:yyyyMMddHHmmss}.hmiproj");
        await File.WriteAllTextAsync(packagePath, json);
        return packagePath;
    }

    /// <summary>
    /// 从指定的 <c>.hmiproj</c> 包文件导入项目，内部复用 <see cref="OpenProjectAsync"/> 逻辑。
    /// </summary>
    /// <param name="packagePath">导入文件的完整路径。</param>
    /// <returns>导入成功后的 <see cref="HmiProject"/> 实例。</returns>
    public async Task<HmiProject> ImportProjectAsync(string packagePath)
    {
        return await OpenProjectAsync(packagePath);
    }
}
