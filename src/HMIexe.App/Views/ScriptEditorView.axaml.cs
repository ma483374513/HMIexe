using Avalonia.Controls;
using Avalonia.Input;
using HMIexe.App.ViewModels;
using HMIexe.Core.Models.Script;

namespace HMIexe.App.Views;

public partial class ScriptEditorView : UserControl
{
    public ScriptEditorView()
    {
        InitializeComponent();
    }

    private void ScriptItem_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border { DataContext: HmiScript script } &&
            DataContext is ScriptEditorViewModel vm)
        {
            vm.SelectedScript = script;
        }
    }
}
