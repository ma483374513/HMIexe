/// <summary>
/// 运行画面播放器主窗口代码后端。
/// </summary>
using Avalonia.Controls;

namespace HMIexe.RuntimePlayer.Views;

/// <summary>
/// HMI 运行画面播放器主窗口。
/// 界面逻辑由 PlayerViewModel 驱动，此文件仅包含初始化代码。
/// </summary>
public partial class PlayerWindow : Window
{
    /// <summary>
    /// 初始化 <see cref="PlayerWindow"/> 的新实例。
    /// </summary>
    public PlayerWindow()
    {
        InitializeComponent();
    }
}
