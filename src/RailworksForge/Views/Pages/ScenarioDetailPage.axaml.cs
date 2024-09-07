using System;
using System.ComponentModel;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Input;

using RailworksForge.Core.Models;
using RailworksForge.ViewModels;

namespace RailworksForge.Views.Pages;

public partial class ScenarioDetailPage : UserControl
{

    public ScenarioDetailPage()
    {
        InitializeComponent();

        if (DataContext is ScenarioDetailViewModel context)
        {
            context.PropertyChanged += ViewModel_PropertyChanged;
        }
    }

    // ReSharper disable once UnusedParameter.Local
    private void DataGrid_OnDoubleTapped(object? _, TappedEventArgs args)
    {
        if (DataContext is ScenarioDetailViewModel context)
        {
            context.ClickedConsistCommand.Execute().Subscribe();
        }
    }

    // ReSharper disable once UnusedParameter.Local
    private void DataGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not DataGrid dataGrid) return;
        if (DataContext is not ScenarioDetailViewModel context) return;

        if (dataGrid.SelectedItems is null) return;

        context.SelectedConsists = dataGrid.SelectedItems.Cast<Consist>();
    }

    // ReSharper disable once UnusedParameter.Local
    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs? e)
    {
        if (e?.PropertyName != "Services")
        {
            return;
        }

        ServicesDataGrid.UpdateLayout();
    }

    private void ServicesDataGrid_OnSelectionChanging(object? sender, CancelEventArgs e)
    {
        if (DataContext is not ScenarioDetailViewModel context) return;

        var selectedItems = ServicesDataGrid.RowSelection?.SelectedItems.Cast<Consist>();

        context.SelectedConsists = selectedItems ?? [];
    }
}
