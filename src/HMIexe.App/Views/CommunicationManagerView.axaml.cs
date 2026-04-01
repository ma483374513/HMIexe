using Avalonia.Controls;
using Avalonia.Input;
using HMIexe.App.ViewModels;
using HMIexe.Core.Models.Communication;

namespace HMIexe.App.Views;

public partial class CommunicationManagerView : UserControl
{
    public CommunicationManagerView()
    {
        InitializeComponent();
    }

    private void ChannelItem_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border { DataContext: CommunicationChannel ch } &&
            DataContext is CommunicationManagerViewModel vm)
        {
            vm.SelectedChannel = ch;
        }
    }
}
