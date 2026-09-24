using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using RailworksForge.Core.Models;
using RailworksForge.Util;
using RailworksForge.ViewModels;

namespace RailworksForge.Views.Pages;

public partial class ConsistDetailPage : UserControl
{
    private readonly DataGridSortHandler _consistVehiclesDataGridSortHandler;
    private readonly DataGridSortHandler _availableStockDataGridSortHandler;

    public ConsistDetailPage()
    {
        InitializeComponent();

        _consistVehiclesDataGridSortHandler = new (ConsistVehiclesDataGrid);
        _availableStockDataGridSortHandler = new (AvailableStockDataGrid);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _consistVehiclesDataGridSortHandler.SortColumns();
        _availableStockDataGridSortHandler.SortColumns();
    }

    // ReSharper disable once UnusedParameter.Local
    private void ConsistVehiclesDataGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not ConsistDetailViewModel context)
        {
            return;
        }

        context.SelectedConsistVehicles = ConsistVehiclesDataGrid.SelectedItems.Cast<ConsistRailVehicle>().ToList();
    }

    // ReSharper disable once UnusedParameter.Local
    private void ConsistVehiclesDataGrid_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not RouteDetailViewModel context) return;

        context.DetailsClickedCommand.Execute().Subscribe(new Subject<Unit>());
    }
}
