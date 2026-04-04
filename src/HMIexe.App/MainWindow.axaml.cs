/// <summary>
/// 应用程序主窗口的代码隐藏文件。
/// 对应 MainWindow.axaml，负责初始化窗口组件。
/// </summary>
using Avalonia.Controls;

namespace HMIexe.App;

/// <summary>
/// HMI 组态软件的主窗口类。
/// 作为应用程序的顶级窗口，其 DataContext 绑定到 <see cref="ViewModels.MainWindowViewModel"/>。
/// 布局与样式由对应的 MainWindow.axaml 文件定义。
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// 初始化主窗口，调用由 XAML 编译器生成的 <see cref="InitializeComponent"/> 方法加载界面组件。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
    }
}