/// <summary>
/// 通信管理视图的代码后端文件。
/// 负责处理通信通道列表中的用户交互，包括鼠标点击选中通信通道项。
/// </summary>
using Avalonia.Controls;
using Avalonia.Input;
using HMIexe.App.ViewModels;
using HMIexe.Core.Models.Communication;

namespace HMIexe.App.Views;

/// <summary>
/// 通信管理器视图，用于展示和管理 HMI 通信通道列表。
/// 提供通信通道的选中交互逻辑，配合 <see cref="CommunicationManagerViewModel"/> 使用。
/// </summary>
public partial class CommunicationManagerView : UserControl
{
    /// <summary>
    /// 初始化 <see cref="CommunicationManagerView"/> 的新实例，并加载 AXAML 生成的组件。
    /// </summary>
    public CommunicationManagerView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 处理通信通道列表项的鼠标按下事件。
    /// 当用户点击某个通信通道条目时，将对应的 <see cref="CommunicationChannel"/> 设置为 ViewModel 中的选中项。
    /// </summary>
    /// <param name="sender">触发事件的控件，预期为绑定了 <see cref="CommunicationChannel"/> 的 <see cref="Border"/>。</param>
    /// <param name="e">鼠标按下事件参数。</param>
    private void ChannelItem_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // 确认发送者是绑定了通信通道的 Border，且当前 DataContext 是通信管理器 ViewModel
        if (sender is Border { DataContext: CommunicationChannel ch } &&
            DataContext is CommunicationManagerViewModel vm)
        {
            vm.SelectedChannel = ch;
        }
    }
}
