/// <summary>
/// 资源管理视图的代码后端文件。
/// 提供 HMI 项目资源（图片、字体等）的管理界面，当前仅包含视图初始化逻辑。
/// </summary>
using Avalonia.Controls;

namespace HMIexe.App.Views;

/// <summary>
/// 资源管理器视图，用于管理 HMI 项目中使用的各类资源文件（如图片、字体等）。
/// 界面逻辑由对应的 ViewModel 驱动，当前代码后端仅执行组件初始化。
/// </summary>
public partial class ResourceManagerView : UserControl
{
    /// <summary>
    /// 初始化 <see cref="ResourceManagerView"/> 的新实例，并加载 AXAML 生成的组件。
    /// </summary>
    public ResourceManagerView()
    {
        InitializeComponent();
    }
}
