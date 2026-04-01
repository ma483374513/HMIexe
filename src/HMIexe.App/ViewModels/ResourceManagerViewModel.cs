using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HMIexe.App.Services;
using HMIexe.Core.Models.Resource;

namespace HMIexe.App.ViewModels;

public partial class ResourceManagerViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;

    public ObservableCollection<HmiResource> Resources { get; } = new();

    [ObservableProperty]
    private HmiResource? _selectedResource;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedTypeFilter = "全部";

    public IReadOnlyList<string> TypeFilters { get; } = new[] { "全部" }.Concat(Enum.GetNames<ResourceType>()).ToList();

    public IEnumerable<HmiResource> FilteredResources =>
        Resources.Where(r =>
            (SelectedTypeFilter == "全部" || r.Type.ToString() == SelectedTypeFilter) &&
            (string.IsNullOrEmpty(SearchText) ||
             r.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
             r.FilePath.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));

    public ResourceManagerViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    partial void OnSearchTextChanged(string value) => OnPropertyChanged(nameof(FilteredResources));
    partial void OnSelectedTypeFilterChanged(string value) => OnPropertyChanged(nameof(FilteredResources));

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

    [RelayCommand]
    private async Task RemoveResource()
    {
        if (SelectedResource == null) return;
        if (SelectedResource.UsedByControlIds.Count > 0)
        {
            var confirm = await _dialogService.ShowConfirmAsync("删除资源",
                $"资源 '{SelectedResource.Name}' 被 {SelectedResource.UsedByControlIds.Count} 个控件引用，确认删除？");
            if (!confirm) return;
        }
        Resources.Remove(SelectedResource);
        SelectedResource = Resources.FirstOrDefault();
        OnPropertyChanged(nameof(FilteredResources));
    }

    [RelayCommand]
    private void CleanUnused()
    {
        var unused = Resources.Where(r => r.UsedByControlIds.Count == 0).ToList();
        foreach (var r in unused)
            Resources.Remove(r);
        OnPropertyChanged(nameof(FilteredResources));
    }

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

    public void LoadFromProject(IEnumerable<HmiResource> resources)
    {
        Resources.Clear();
        foreach (var r in resources)
            Resources.Add(r);
        SelectedResource = Resources.FirstOrDefault();
        OnPropertyChanged(nameof(FilteredResources));
    }
}
