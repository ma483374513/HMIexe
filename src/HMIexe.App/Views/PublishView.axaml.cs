/// <summary>
/// 工程发布视图的代码后端文件。
/// 负责在日志输出区域内容更新时自动滚动到末尾，以便用户实时跟踪发布进度。
/// </summary>
using Avalonia.Controls;
using System.Collections.Specialized;
using HMIexe.App.ViewModels;

namespace HMIexe.App.Views;

/// <summary>
/// 工程发布视图，展示发布参数配置界面和实时日志输出。
/// 配合 <see cref="PublishViewModel"/> 使用。
/// </summary>
public partial class PublishView : UserControl
{
    /// <summary>上一个已订阅的 ViewModel，用于在 DataContext 切换时解除旧订阅，防止内存泄漏。</summary>
    private PublishViewModel? _previousVm;

    /// <summary>
    /// 初始化 <see cref="PublishView"/> 的新实例，并注册 DataContext 变更事件。
    /// </summary>
    public PublishView()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>
    /// DataContext 变更事件处理器。
    /// 先解除旧 ViewModel 的日志集合订阅，再订阅新 ViewModel，防止重复订阅或内存泄漏。
    /// </summary>
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_previousVm != null)
            _previousVm.LogLines.CollectionChanged -= OnLogLinesChanged;

        _previousVm = DataContext as PublishViewModel;

        if (_previousVm != null)
            _previousVm.LogLines.CollectionChanged += OnLogLinesChanged;
    }

    /// <summary>
    /// 日志集合变更事件处理器。
    /// 每当新增一条日志时，将日志滚动区域滚动至底部。
    /// </summary>
    private void OnLogLinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            var scroller = this.FindControl<ScrollViewer>("LogScrollViewer");
            scroller?.ScrollToEnd();
        }
    }
}
