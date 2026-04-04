/// <summary>
/// 变量管理视图的代码后端文件。
/// 提供 HMI 项目变量的管理界面，当前仅包含视图初始化逻辑。
/// </summary>
using Avalonia.Controls;

namespace HMIexe.App.Views;

/// <summary>
/// 变量管理器视图，用于管理 HMI 项目中定义的变量（标签）。
/// 界面逻辑由对应的 ViewModel 驱动，当前代码后端仅执行组件初始化。
/// </summary>
public partial class VariableManagerView : UserControl
{
    /// <summary>
    /// 初始化 <see cref="VariableManagerView"/> 的新实例，并加载 AXAML 生成的组件。
    /// </summary>
    public VariableManagerView()
    {
        InitializeComponent();
    }
}
