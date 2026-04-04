/// <summary>
/// 脚本编辑器视图的代码后端文件。
/// 负责处理脚本列表中的用户交互，包括鼠标点击选中脚本项。
/// </summary>
using Avalonia.Controls;
using Avalonia.Input;
using HMIexe.App.ViewModels;
using HMIexe.Core.Models.Script;

namespace HMIexe.App.Views;

/// <summary>
/// 脚本编辑器视图，用于展示和编辑 HMI 脚本列表。
/// 提供脚本条目的选中交互逻辑，配合 <see cref="ScriptEditorViewModel"/> 使用。
/// </summary>
public partial class ScriptEditorView : UserControl
{
    /// <summary>
    /// 初始化 <see cref="ScriptEditorView"/> 的新实例，并加载 AXAML 生成的组件。
    /// </summary>
    public ScriptEditorView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 处理脚本列表项的鼠标按下事件。
    /// 当用户点击某个脚本条目时，将对应的 <see cref="HmiScript"/> 设置为 ViewModel 中的选中项，
    /// 以便在编辑区域中加载并显示该脚本的内容。
    /// </summary>
    /// <param name="sender">触发事件的控件，预期为绑定了 <see cref="HmiScript"/> 的 <see cref="Border"/>。</param>
    /// <param name="e">鼠标按下事件参数。</param>
    private void ScriptItem_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // 确认发送者是绑定了脚本对象的 Border，且当前 DataContext 是脚本编辑器 ViewModel
        if (sender is Border { DataContext: HmiScript script } &&
            DataContext is ScriptEditorViewModel vm)
        {
            vm.SelectedScript = script;
        }
    }
}
