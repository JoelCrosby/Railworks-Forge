using Avalonia.Controls;
using Avalonia.Interactivity;

using RailworksForge.Util;

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
}
