using Avalonia.Controls;
using Avalonia.Input;
using HMIexe.App.ViewModels;
using HMIexe.Core.Models.Alarm;

namespace HMIexe.App.Views;

public partial class AlarmManagerView : UserControl
{
    public AlarmManagerView()
    {
        InitializeComponent();
    }

    private void AlarmDefItem_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border { DataContext: HmiAlarmDefinition def } &&
            DataContext is AlarmManagerViewModel vm)
        {
            vm.SelectedDefinition = def;
        }
    }
}
