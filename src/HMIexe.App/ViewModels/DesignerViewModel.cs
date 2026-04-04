/// <summary>
/// 设计器视图模型文件。
/// 提供 HMI 画布的完整设计功能：多页面/多图层管理、控件增删与选中、
/// 撤销/重做、复制/粘贴、对齐/分布、层级排序及缩放平移控制。
/// </summary>
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HMIexe.Core.Models.Canvas;
using HMIexe.Core.Models.Controls;
using HMIexe.Core.Models.Project;
using HMIexe.Core.UndoRedo;

namespace HMIexe.App.ViewModels;

/// <summary>
/// 设计器视图模型。
/// 管理画布上的页面、图层和控件，并通过 <see cref="UndoRedoHistory"/> 支持
/// 完整的撤销/重做操作。同时集成属性面板 ViewModel 以联动响应控件选中变化。
/// </summary>
public partial class DesignerViewModel : ObservableObject
{
    /// <summary>撤销/重做历史记录管理器，记录所有可逆的画布操作。</summary>
    public UndoRedoHistory UndoRedo { get; } = new();

    /// <summary>当前正在编辑的 HMI 页面。</summary>
    [ObservableProperty]
    private HmiPage? _currentPage;

    /// <summary>当前选中的单个控件（多选时为最后选中的控件）。</summary>
    [ObservableProperty]
    private HmiControlBase? _selectedControl;

    /// <summary>画布缩放级别，范围 0.1～5.0，默认 1.0（100%）。</summary>
    [ObservableProperty]
    private double _zoomLevel = 1.0;

    /// <summary>画布水平平移偏移量（设计坐标系，单位为像素）。</summary>
    [ObservableProperty]
    private double _panOffsetX;

    /// <summary>画布垂直平移偏移量（设计坐标系，单位为像素）。</summary>
    [ObservableProperty]
    private double _panOffsetY;

    /// <summary>是否显示对齐网格线。</summary>
    [ObservableProperty]
    private bool _showGrid = true;

    /// <summary>网格单元格大小（像素），控制网格线间距。</summary>
    [ObservableProperty]
    private double _gridSize = 10;

    /// <summary>是否启用控件移动时的网格吸附功能。</summary>
    [ObservableProperty]
    private bool _snapToGrid = true;

    /// <summary>当前鼠标指针在画布坐标系中的 X 位置，用于坐标显示。</summary>
    [ObservableProperty]
    private double _canvasMouseX;

    /// <summary>当前鼠标指针在画布坐标系中的 Y 位置，用于坐标显示。</summary>
    [ObservableProperty]
    private double _canvasMouseY;

    /// <summary>剪贴板中存储的控件 JSON 序列化数据，用于复制/粘贴操作。</summary>
    private string? _clipboardJson;

    /// <summary>当前多选状态下所有已选中控件的集合。</summary>
    public ObservableCollection<HmiControlBase> SelectedControls { get; } = new();

    /// <summary>当前工程的所有 HMI 页面集合，绑定到页面标签列表。</summary>
    public ObservableCollection<HmiPage> Pages { get; } = new();

    /// <summary>撤销上一步操作命令。</summary>
    [RelayCommand]
    private void Undo() => UndoRedo.Undo();

    /// <summary>重做已撤销的操作命令。</summary>
    [RelayCommand]
    private void Redo() => UndoRedo.Redo();

    /// <summary>放大画布，最大缩放比例为 500%。</summary>
    [RelayCommand]
    private void ZoomIn() => ZoomLevel = Math.Min(ZoomLevel + 0.1, 5.0);

    /// <summary>缩小画布，最小缩放比例为 10%。</summary>
    [RelayCommand]
    private void ZoomOut() => ZoomLevel = Math.Max(ZoomLevel - 0.1, 0.1);

    /// <summary>重置画布缩放为 100%。</summary>
    [RelayCommand]
    private void ResetZoom() => ZoomLevel = 1.0;

    /// <summary>
    /// 添加新页面命令。新页面默认包含一个名为 "Layer 1" 的图层，并自动切换到该页面。
    /// </summary>
    [RelayCommand]
    private void AddPage()
    {
        var page = new HmiPage { Name = $"Page {Pages.Count + 1}" };
        page.Layers.Add(new HmiLayer { Name = "Layer 1" });
        Pages.Add(page);
        CurrentPage = page;
    }

    /// <summary>
    /// 删除指定页面命令。至少保留一个页面，删除后自动切换到相邻页面。
    /// </summary>
    /// <param name="page">要删除的页面。</param>
    [RelayCommand]
    private void RemovePage(HmiPage page)
    {
        if (Pages.Count <= 1) return;
        var idx = Pages.IndexOf(page);
        Pages.Remove(page);
        if (CurrentPage == page)
            CurrentPage = Pages.Count > 0 ? Pages[Math.Max(0, idx - 1)] : null;
    }

    /// <summary>
    /// 在当前页面添加新图层命令。
    /// </summary>
    [RelayCommand]
    private void AddLayer()
    {
        if (CurrentPage == null) return;
        var layer = new HmiLayer { Name = $"Layer {CurrentPage.Layers.Count + 1}" };
        CurrentPage.Layers.Add(layer);
    }

    /// <summary>
    /// 删除指定图层命令。至少保留一个图层。
    /// </summary>
    /// <param name="layer">要删除的图层。</param>
    [RelayCommand]
    private void RemoveLayer(HmiLayer layer)
    {
        if (CurrentPage == null || CurrentPage.Layers.Count <= 1) return;
        CurrentPage.Layers.Remove(layer);
    }

    /// <summary>切换图层的可见/隐藏状态命令。</summary>
    [RelayCommand]
    private void ToggleLayerVisible(HmiLayer layer) => layer.Visible = !layer.Visible;

    /// <summary>切换图层的锁定/解锁状态命令。</summary>
    [RelayCommand]
    private void ToggleLayerLock(HmiLayer layer) => layer.Locked = !layer.Locked;

    /// <summary>Select or add to selection. Pass null to deselect all.</summary>
    public void SelectControl(HmiControlBase? ctrl, bool addToSelection)
    {
        if (!addToSelection)
        {
            foreach (var c in SelectedControls) c.IsSelected = false;
            SelectedControls.Clear();
        }

        if (ctrl == null)
        {
            SelectedControl = null;
            PropertyPanel.SelectedControl = null;
            return;
        }

        if (!SelectedControls.Contains(ctrl))
        {
            ctrl.IsSelected = true;
            SelectedControls.Add(ctrl);
        }

        SelectedControl = ctrl;
        PropertyPanel.SelectedControl = ctrl;
    }

    /// <summary>
    /// 删除所有已选中控件命令。操作通过撤销/重做系统执行，支持撤销。
    /// </summary>
    [RelayCommand]
    private void DeleteSelectedControls()
    {
        if (CurrentPage == null) return;
        var items = SelectedControls
            .SelectMany(ctrl => CurrentPage.Layers
                .Where(l => l.Controls.Contains(ctrl))
                .Select(l => (l, ctrl)))
            .ToList();
        if (items.Count == 0) return;
        var action = new RemoveControlsAction(items);
        UndoRedo.Execute(action);
        foreach (var c in SelectedControls) c.IsSelected = false;
        SelectedControls.Clear();
        SelectedControl = null;
        PropertyPanel.SelectedControl = null;
    }

    /// <summary>
    /// 复制选中控件命令。将选中控件序列化为 JSON 存入剪贴板字段，供粘贴使用。
    /// </summary>
    [RelayCommand]
    private void CopySelectedControls()
    {
        if (SelectedControls.Count == 0) return;
        // 序列化时保留类型名称，粘贴时用于反序列化还原具体控件类型
        var list = SelectedControls.Select(c => new
        {
            TypeName = c.GetType().Name,
            Json = JsonSerializer.Serialize(c, c.GetType())
        }).ToList();
        _clipboardJson = JsonSerializer.Serialize(list);
    }

    /// <summary>
    /// 粘贴控件命令。从剪贴板 JSON 还原控件，分配新 ID，偏移坐标后添加到当前图层。
    /// 整个粘贴操作作为单一的可撤销动作记录到历史栈。
    /// </summary>
    [RelayCommand]
    private void PasteControls()
    {
        if (CurrentPage == null || string.IsNullOrEmpty(_clipboardJson)) return;
        var layer = CurrentPage.Layers.FirstOrDefault();
        if (layer == null) return;

        var list = JsonSerializer.Deserialize<List<ClipboardEntry>>(_clipboardJson);
        if (list == null) return;

        // 在 Controls 命名空间内查找所有具体控件类型，构建类型名称到 Type 的映射字典
        var controlTypes = typeof(HmiControlBase).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(HmiControlBase)))
            .ToDictionary(t => t.Name, t => t);

        var pasted = new List<HmiControlBase>();
        foreach (var entry in list)
        {
            if (!controlTypes.TryGetValue(entry.TypeName, out var type)) continue;
            if (JsonSerializer.Deserialize(entry.Json, type) is not HmiControlBase ctrl) continue;
            // 粘贴的控件分配新 ID，并向右下方偏移 20px 以与原控件错开
            ctrl.Id = Guid.NewGuid().ToString();
            ctrl.X += 20;
            ctrl.Y += 20;
            ctrl.IsSelected = false;
            pasted.Add(ctrl);
        }

        if (pasted.Count == 0) return;

        // 将多个添加操作包装为单一的可撤销组合动作
        var addActions = pasted.Select(c => new AddControlAction(layer, c)).ToList();
        UndoRedo.Execute(new SetPropertyAction(
            $"粘贴 {pasted.Count} 个控件",
            () => { foreach (var a in addActions) a.Execute(); },
            () => { foreach (var a in addActions) a.Undo(); }
        ));

        foreach (var c in SelectedControls) c.IsSelected = false;
        SelectedControls.Clear();
        foreach (var c in pasted)
        {
            c.IsSelected = true;
            SelectedControls.Add(c);
        }
        SelectedControl = pasted.LastOrDefault();
        PropertyPanel.SelectedControl = SelectedControl;
    }

    /// <summary>
    /// 左对齐命令。将所有选中控件的左边缘对齐到最左侧控件的 X 坐标。
    /// </summary>
    [RelayCommand]
    private void AlignLeft()
    {
        if (SelectedControls.Count < 2) return;
        var minX = SelectedControls.Min(c => c.X);
        foreach (var c in SelectedControls) c.X = minX;
    }

    /// <summary>
    /// 右对齐命令。将所有选中控件的右边缘对齐到最右侧控件的右边缘。
    /// </summary>
    [RelayCommand]
    private void AlignRight()
    {
        if (SelectedControls.Count < 2) return;
        var maxRight = SelectedControls.Max(c => c.X + c.Width);
        foreach (var c in SelectedControls) c.X = maxRight - c.Width;
    }

    /// <summary>
    /// 顶端对齐命令。将所有选中控件的上边缘对齐到最顶部控件的 Y 坐标。
    /// </summary>
    [RelayCommand]
    private void AlignTop()
    {
        if (SelectedControls.Count < 2) return;
        var minY = SelectedControls.Min(c => c.Y);
        foreach (var c in SelectedControls) c.Y = minY;
    }

    /// <summary>
    /// 底端对齐命令。将所有选中控件的下边缘对齐到最底部控件的下边缘。
    /// </summary>
    [RelayCommand]
    private void AlignBottom()
    {
        if (SelectedControls.Count < 2) return;
        var maxBottom = SelectedControls.Max(c => c.Y + c.Height);
        foreach (var c in SelectedControls) c.Y = maxBottom - c.Height;
    }

    /// <summary>
    /// 水平居中对齐命令。将所有选中控件的垂直中心对齐到所有控件垂直中心的平均位置。
    /// </summary>
    [RelayCommand]
    private void AlignCenterHorizontal()
    {
        if (SelectedControls.Count < 2) return;
        var avgCenterY = SelectedControls.Average(c => c.Y + c.Height / 2);
        foreach (var c in SelectedControls) c.Y = avgCenterY - c.Height / 2;
    }

    /// <summary>
    /// 垂直居中对齐命令。将所有选中控件的水平中心对齐到所有控件水平中心的平均位置。
    /// </summary>
    [RelayCommand]
    private void AlignCenterVertical()
    {
        if (SelectedControls.Count < 2) return;
        var avgCenterX = SelectedControls.Average(c => c.X + c.Width / 2);
        foreach (var c in SelectedControls) c.X = avgCenterX - c.Width / 2;
    }

    /// <summary>
    /// 水平等间距分布命令。在最左和最右控件之间均匀分布中间控件，至少需要 3 个控件。
    /// </summary>
    [RelayCommand]
    private void DistributeHorizontally()
    {
        if (SelectedControls.Count < 3) return;
        var sorted = SelectedControls.OrderBy(c => c.X).ToList();
        var totalWidth = sorted.Last().X + sorted.Last().Width - sorted.First().X;
        var controlsWidth = sorted.Sum(c => c.Width);
        // 计算均匀分布所需的间隙宽度
        var gap = (totalWidth - controlsWidth) / (sorted.Count - 1);
        var x = sorted.First().X;
        foreach (var c in sorted)
        {
            c.X = x;
            x += c.Width + gap;
        }
    }

    /// <summary>
    /// 垂直等间距分布命令。在最上和最下控件之间均匀分布中间控件，至少需要 3 个控件。
    /// </summary>
    [RelayCommand]
    private void DistributeVertically()
    {
        if (SelectedControls.Count < 3) return;
        var sorted = SelectedControls.OrderBy(c => c.Y).ToList();
        var totalHeight = sorted.Last().Y + sorted.Last().Height - sorted.First().Y;
        var controlsHeight = sorted.Sum(c => c.Height);
        // 计算均匀分布所需的间隙高度
        var gap = (totalHeight - controlsHeight) / (sorted.Count - 1);
        var y = sorted.First().Y;
        foreach (var c in sorted)
        {
            c.Y = y;
            y += c.Height + gap;
        }
    }

    /// <summary>
    /// 置于最顶层命令。将选中控件的 ZIndex 设为当前页面所有控件中最大值加 1。
    /// </summary>
    [RelayCommand]
    private void BringToFront()
    {
        if (SelectedControl == null || CurrentPage == null) return;
        var allControls = CurrentPage.AllControls.ToList();
        var maxZ = allControls.Count > 0 ? allControls.Max(c => c.ZIndex) : 0;
        SelectedControl.ZIndex = maxZ + 1;
    }

    /// <summary>
    /// 置于最底层命令。将选中控件的 ZIndex 设为当前页面所有控件中最小值减 1。
    /// </summary>
    [RelayCommand]
    private void SendToBack()
    {
        if (SelectedControl == null || CurrentPage == null) return;
        var allControls = CurrentPage.AllControls.ToList();
        var minZ = allControls.Count > 0 ? allControls.Min(c => c.ZIndex) : 0;
        SelectedControl.ZIndex = minZ - 1;
    }

    /// <summary>
    /// 上移一层命令。将选中控件的 ZIndex 增加 1。
    /// </summary>
    [RelayCommand]
    private void BringForward()
    {
        if (SelectedControl == null) return;
        SelectedControl.ZIndex++;
    }

    /// <summary>
    /// 下移一层命令。将选中控件的 ZIndex 减少 1。
    /// </summary>
    [RelayCommand]
    private void SendBackward()
    {
        if (SelectedControl == null) return;
        SelectedControl.ZIndex--;
    }

    /// <summary>
    /// 属性面板视图模型，与设计器联动显示选中控件的属性。
    /// </summary>
    public PropertyPanelViewModel PropertyPanel { get; }

    /// <summary>
    /// 初始化设计器视图模型。
    /// </summary>
    /// <param name="variableService">变量服务，传递给属性面板用于变量绑定下拉列表（可选）。</param>
    public DesignerViewModel(HMIexe.Core.Services.IVariableService? variableService = null)
    {
        PropertyPanel = new PropertyPanelViewModel(UndoRedo, variableService);
    }

    /// <summary>
    /// 选中控件变更时同步更新属性面板的目标控件。
    /// </summary>
    partial void OnSelectedControlChanged(HmiControlBase? value)
    {
        PropertyPanel.SelectedControl = value;
    }

    /// <summary>
    /// 当前页面变更时同步更新属性面板的当前页面引用。
    /// </summary>
    partial void OnCurrentPageChanged(HmiPage? value)
    {
        PropertyPanel.CurrentPage = value;
    }

    /// <summary>
    /// 从工程对象加载所有页面，清空当前选中和撤销历史，并切换到工程默认页面。
    /// </summary>
    /// <param name="project">要加载的 HMI 工程对象。</param>
    public void LoadProject(HmiProject project)
    {
        Pages.Clear();
        foreach (var c in SelectedControls) c.IsSelected = false;
        SelectedControls.Clear();
        SelectedControl = null;
        UndoRedo.Clear();

        foreach (var page in project.Pages)
            Pages.Add(page);

        // 优先切换到工程指定的默认页面，否则使用第一个页面
        CurrentPage = Pages.FirstOrDefault(p => p.Id == project.DefaultPageId)
            ?? Pages.FirstOrDefault();
    }

    /// <summary>
    /// 切换当前编辑页面命令，切换时清空控件选中状态。
    /// </summary>
    /// <param name="page">要切换到的目标页面。</param>
    [RelayCommand]
    private void SelectPage(HmiPage page)
    {
        CurrentPage = page;
        foreach (var c in SelectedControls) c.IsSelected = false;
        SelectedControls.Clear();
        SelectedControl = null;
    }

    /// <summary>
    /// 向当前页面的第一个图层添加指定类型控件的命令。
    /// 控件位置根据图层已有控件数量进行阶梯偏移，避免完全重叠。
    /// </summary>
    /// <param name="controlTypeStr">控件类型字符串标识（例如 "Button"、"Gauge"）。</param>
    [RelayCommand]
    private void AddControlToCanvas(string controlTypeStr)
    {
        if (CurrentPage == null) return;
        var layer = CurrentPage.Layers.FirstOrDefault();
        if (layer == null) return;

        HmiControlBase? control = controlTypeStr switch
        {
            "Button" => new Core.Models.Controls.ButtonControl { Name = $"Button{layer.Controls.Count + 1}" },
            "Label" => new Core.Models.Controls.LabelControl { Name = $"Label{layer.Controls.Count + 1}" },
            "TextBox" => new Core.Models.Controls.TextBoxControl { Name = $"TextBox{layer.Controls.Count + 1}" },
            "Gauge" => new Core.Models.Controls.GaugeControl { Name = $"Gauge{layer.Controls.Count + 1}" },
            "IndicatorLight" => new Core.Models.Controls.IndicatorLightControl { Name = $"Light{layer.Controls.Count + 1}" },
            "Switch" => new Core.Models.Controls.SwitchControl { Name = $"Switch{layer.Controls.Count + 1}" },
            "Slider" => new Core.Models.Controls.SliderControl { Name = $"Slider{layer.Controls.Count + 1}" },
            "Line" => new Core.Models.Controls.LineControl { Name = $"Line{layer.Controls.Count + 1}" },
            "Rectangle" => new Core.Models.Controls.RectangleControl { Name = $"Rect{layer.Controls.Count + 1}" },
            "Circle" => new Core.Models.Controls.CircleControl { Name = $"Circle{layer.Controls.Count + 1}" },
            "Ellipse" => new Core.Models.Controls.EllipseControl { Name = $"Ellipse{layer.Controls.Count + 1}" },
            _ => null
        };

        if (control == null) return;
        // 按图层中已有控件数量阶梯偏移初始位置，避免新控件完全遮盖已有控件
        control.X = 100 + layer.Controls.Count * 10;
        control.Y = 100 + layer.Controls.Count * 10;
        var action = new AddControlAction(layer, control);
        UndoRedo.Execute(action);
        SelectControl(control, false);
    }

    /// <summary>剪贴板序列化条目，保存控件的类型名称和 JSON 数据。</summary>
    private record ClipboardEntry(string TypeName, string Json);
}
