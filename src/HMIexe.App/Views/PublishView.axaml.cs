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
    /// <summary>
    /// 初始化 <see cref="PublishView"/> 的新实例，并注册日志集合变更事件以实现自动滚动。
    /// </summary>
    public PublishView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is PublishViewModel vm)
            {
                vm.LogLines.CollectionChanged += OnLogLinesChanged;
            }
        };
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
