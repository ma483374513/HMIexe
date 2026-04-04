/// <summary>
/// 运行时视图的代码后端文件。
/// 提供 HMI 项目的运行时预览或执行界面，当前仅包含视图初始化逻辑。
/// </summary>
using Avalonia.Controls;

namespace HMIexe.App.Views;

/// <summary>
/// 运行时视图，用于展示 HMI 项目在运行状态下的界面效果。
/// 界面逻辑由对应的 ViewModel 驱动，当前代码后端仅执行组件初始化。
/// </summary>
public partial class RuntimeView : UserControl
{
    /// <summary>
    /// 初始化 <see cref="RuntimeView"/> 的新实例，并加载 AXAML 生成的组件。
    /// </summary>
    public RuntimeView()
    {
        InitializeComponent();
    }
}
