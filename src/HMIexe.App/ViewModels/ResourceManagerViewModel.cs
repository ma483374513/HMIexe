/// <summary>
/// 资源管理器视图模型文件。
/// 负责工程媒体资源（图片、音频、视频、字体等）的导入、筛选、删除和清理操作。
/// </summary>
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HMIexe.App.Services;
using HMIexe.Core.Models.Resource;

namespace HMIexe.App.ViewModels;

/// <summary>
/// 资源管理器视图模型。
/// 提供工程资源文件的导入管理，支持按类型和名称筛选，
/// 以及删除单个资源和一键清除未引用资源的功能。
/// </summary>
public partial class ResourceManagerViewModel : ObservableObject
{
    /// <summary>对话框服务，用于文件选择和操作确认。</summary>
    private readonly IDialogService _dialogService;

    /// <summary>已导入的全部资源集合，作为过滤的数据源。</summary>
    public ObservableCollection<HmiResource> Resources { get; } = new();

    /// <summary>当前在资源列表中选中的资源项。</summary>
    [ObservableProperty]
    private HmiResource? _selectedResource;

    /// <summary>搜索关键字，变更时自动触发筛选刷新。</summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>当前选中的资源类型过滤条件，变更时自动触发筛选刷新。</summary>
    [ObservableProperty]
    private string _selectedTypeFilter = "全部";

    /// <summary>资源类型过滤器列表，包含"全部"及所有 <see cref="ResourceType"/> 枚举名称。</summary>
    public IReadOnlyList<string> TypeFilters { get; } = new[] { "全部" }.Concat(Enum.GetNames<ResourceType>()).ToList();

    /// <summary>
    /// 经过类型和关键字双重筛选后的资源集合，绑定到资源列表视图。
    /// 类型不匹配或名称/路径均不包含搜索关键字的资源将被排除。
    /// </summary>
    public IEnumerable<HmiResource> FilteredResources =>
        Resources.Where(r =>
            (SelectedTypeFilter == "全部" || r.Type.ToString() == SelectedTypeFilter) &&
            (string.IsNullOrEmpty(SearchText) ||
             r.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
             r.FilePath.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));

    /// <summary>
    /// 初始化资源管理器视图模型。
    /// </summary>
    /// <param name="dialogService">UI 对话框服务。</param>
    public ResourceManagerViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    /// <summary>搜索文本变更时通知 <see cref="FilteredResources"/> 属性更新。</summary>
    partial void OnSearchTextChanged(string value) => OnPropertyChanged(nameof(FilteredResources));

    /// <summary>类型过滤条件变更时通知 <see cref="FilteredResources"/> 属性更新。</summary>
    partial void OnSelectedTypeFilterChanged(string value) => OnPropertyChanged(nameof(FilteredResources));

    /// <summary>
    /// 导入资源命令。弹出文件选择对话框，将选中的媒体文件添加到资源集合中。
    /// 若同路径文件已存在则提示重复，不重复添加。
    /// </summary>
    [RelayCommand]
    private async Task ImportResources()
    {
        var extensions = new[] { "png", "jpg", "jpeg", "gif", "bmp", "svg", "mp3", "wav", "mp4", "ttf", "otf" };
        var path = await _dialogService.OpenFileAsync("导入资源",
            [new FileFilter("支持的文件", extensions)]);
        if (string.IsNullOrEmpty(path)) return;

        var info = new FileInfo(path);
        var resource = new HmiResource
        {
            Name = info.Name,
            FilePath = path,
            FileSize = info.Exists ? info.Length : 0,
            Type = DetermineType(info.Extension),
            ImportedAt = DateTime.Now
        };

        if (!Resources.Any(r => r.FilePath == path))
        {
            Resources.Add(resource);
            SelectedResource = resource;
            OnPropertyChanged(nameof(FilteredResources));
        }
        else
        {
            await _dialogService.ShowMessageAsync("提示", "该资源已存在");
        }
    }

    /// <summary>
    /// 删除选中资源命令。若资源被控件引用，先显示确认对话框再删除。
    /// </summary>
    [RelayCommand]
    private async Task RemoveResource()
    {
        if (SelectedResource == null) return;
        if (SelectedResource.UsedByControlIds.Count > 0)
        {
            // 资源仍被控件引用时需二次确认，防止意外删除导致控件缺失资源
            var confirm = await _dialogService.ShowConfirmAsync("删除资源",
                $"资源 '{SelectedResource.Name}' 被 {SelectedResource.UsedByControlIds.Count} 个控件引用，确认删除？");
            if (!confirm) return;
        }
        Resources.Remove(SelectedResource);
        SelectedResource = Resources.FirstOrDefault();
        OnPropertyChanged(nameof(FilteredResources));
    }

    /// <summary>
    /// 清除未使用资源命令。批量移除所有未被任何控件引用的资源，释放资源列表空间。
    /// </summary>
    [RelayCommand]
    private void CleanUnused()
    {
        var unused = Resources.Where(r => r.UsedByControlIds.Count == 0).ToList();
        foreach (var r in unused)
            Resources.Remove(r);
        OnPropertyChanged(nameof(FilteredResources));
    }

    /// <summary>
    /// 根据文件扩展名推断资源类型。
    /// </summary>
    /// <param name="extension">文件扩展名（含点号，例如 ".png"）。</param>
    /// <returns>对应的 <see cref="ResourceType"/> 枚举值。</returns>
    private static ResourceType DetermineType(string extension) =>
        extension.ToLowerInvariant() switch
        {
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" => ResourceType.Image,
            ".svg" => ResourceType.Svg,
            ".mp3" or ".wav" or ".ogg" => ResourceType.Audio,
            ".mp4" or ".avi" or ".mkv" => ResourceType.Video,
            ".ttf" or ".otf" or ".woff" => ResourceType.Font,
            _ => ResourceType.Other
        };

    /// <summary>
    /// 从工程数据加载资源列表，替换当前内容。
    /// </summary>
    /// <param name="resources">工程中保存的资源集合。</param>
    public void LoadFromProject(IEnumerable<HmiResource> resources)
    {
        Resources.Clear();
        foreach (var r in resources)
            Resources.Add(r);
        SelectedResource = Resources.FirstOrDefault();
        OnPropertyChanged(nameof(FilteredResources));
    }
}
