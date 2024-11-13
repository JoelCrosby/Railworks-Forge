using System;
using System.ComponentModel;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Input;

using RailworksForge.Controls;
using RailworksForge.ViewModels;

namespace RailworksForge.Views.Pages;

public partial class ScenarioDetailPage : DataGridUserControl
{
    protected override DataGrid DataGrid => ServicesDataGrid;

    public ScenarioDetailPage()
    {
        InitializeComponent();
        SortColumns();

        if (DataContext is ScenarioDetailViewModel context)
        {
            context.PropertyChanged += ViewModel_PropertyChanged;
        }
    }

    // ReSharper disable once UnusedParameter.Local
    private void DataGrid_OnDoubleTapped(object? _, TappedEventArgs args)
    {
        if (args.Source is not Border) return;

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

        context.SelectedConsistViewModels = dataGrid.SelectedItems.Cast<ConsistViewModel>();
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
}
