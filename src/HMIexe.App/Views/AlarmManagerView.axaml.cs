/// <summary>
/// 报警管理视图的代码后端文件。
/// 负责处理报警定义列表中的用户交互，包括鼠标点击选中报警定义项。
/// </summary>
using Avalonia.Controls;
using Avalonia.Input;
using HMIexe.App.ViewModels;
using HMIexe.Core.Models.Alarm;

namespace HMIexe.App.Views;

/// <summary>
/// 报警管理器视图，用于展示和管理 HMI 报警定义列表。
/// 提供报警定义的选中交互逻辑，配合 <see cref="AlarmManagerViewModel"/> 使用。
/// </summary>
public partial class AlarmManagerView : UserControl
{
    /// <summary>
    /// 初始化 <see cref="AlarmManagerView"/> 的新实例，并加载 AXAML 生成的组件。
    /// </summary>
    public AlarmManagerView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 处理报警定义列表项的鼠标按下事件。
    /// 当用户点击某个报警定义条目时，将对应的 <see cref="HmiAlarmDefinition"/> 设置为 ViewModel 中的选中项。
    /// </summary>
    /// <param name="sender">触发事件的控件，预期为绑定了 <see cref="HmiAlarmDefinition"/> 的 <see cref="Border"/>。</param>
    /// <param name="e">鼠标按下事件参数。</param>
    private void AlarmDefItem_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // 确认发送者是绑定了报警定义的 Border，且当前 DataContext 是报警管理器 ViewModel
        if (sender is Border { DataContext: HmiAlarmDefinition def } &&
            DataContext is AlarmManagerViewModel vm)
        {
            vm.SelectedDefinition = def;
        }
    }
}
