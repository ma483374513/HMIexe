using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HMIexe.Core.Models.Canvas;
using HMIexe.Core.Models.Controls;
using HMIexe.Core.Models.Variables;
using HMIexe.Core.Services;

namespace HMIexe.App.ViewModels;

/// <summary>
/// View model for runtime preview mode. Applies variable bindings to controls and
/// refreshes control values as variables change.
/// </summary>
public partial class RuntimeViewModel : ObservableObject
{
    private readonly IVariableService _variableService;

    [ObservableProperty]
    private HmiPage? _currentPage;

    [ObservableProperty]
    private double _zoomLevel = 1.0;

    [ObservableProperty]
    private string _statusText = "预览运行中";

    public ObservableCollection<HmiPage> Pages { get; } = new();

    public RuntimeViewModel(IVariableService variableService)
    {
        _variableService = variableService;
        _variableService.VariableValueChanged += OnVariableValueChanged;
    }

    public void LoadPages(IEnumerable<HmiPage> pages, IEnumerable<HmiVariable> variables)
    {
        Pages.Clear();
        foreach (var p in pages)
            Pages.Add(p);
        CurrentPage = Pages.FirstOrDefault();

        // Apply initial bindings using already-registered variables
        if (CurrentPage != null)
            ApplyBindings(CurrentPage);
    }

    [RelayCommand]
    private void SelectPage(HmiPage page)
    {
        CurrentPage = page;
        if (page != null)
            ApplyBindings(page);
    }

    [RelayCommand]
    private void ZoomIn() => ZoomLevel = Math.Min(ZoomLevel + 0.1, 4.0);

    [RelayCommand]
    private void ZoomOut() => ZoomLevel = Math.Max(ZoomLevel - 0.1, 0.1);

    [RelayCommand]
    private void ResetZoom() => ZoomLevel = 1.0;

    private void OnVariableValueChanged(object? sender, VariableValueChangedEventArgs e)
    {
        if (CurrentPage == null) return;
        foreach (var ctrl in CurrentPage.AllControls)
        {
            if (ctrl.ValueBindingVariable == e.VariableName)
                ApplyValueToControl(ctrl, e.NewValue);
        }
    }

    private void ApplyBindings(HmiPage page)
    {
        foreach (var ctrl in page.AllControls)
        {
            if (string.IsNullOrEmpty(ctrl.ValueBindingVariable)) continue;
            var variable = _variableService.GetVariableByName(ctrl.ValueBindingVariable);
            if (variable != null)
                ApplyValueToControl(ctrl, variable.Value);
        }
    }

    private static void ApplyValueToControl(HmiControlBase ctrl, object? value)
    {
        try
        {
            switch (ctrl)
            {
                case GaugeControl g:
                    g.Value = Convert.ToDouble(value ?? 0);
                    break;
                case SliderControl s:
                    s.Value = Convert.ToDouble(value ?? 0);
                    break;
                case IndicatorLightControl light:
                    light.IsOn = Convert.ToBoolean(value ?? false);
                    break;
                case SwitchControl sw:
                    sw.IsOn = Convert.ToBoolean(value ?? false);
                    break;
                case LabelControl lbl:
                    lbl.Text = value?.ToString() ?? string.Empty;
                    break;
                case TextBoxControl tb:
                    tb.Text = value?.ToString() ?? string.Empty;
                    break;
            }
        }
        catch (InvalidCastException)
        {
            // Ignore type conversion errors between variable type and control value type
        }
        catch (FormatException)
        {
            // Ignore format errors when converting variable string values
        }
        catch (OverflowException)
        {
            // Ignore overflow when converting numeric values
        }
    }
}
