using Avalonia.Controls;
using Avalonia.Interactivity;

using RailworksForge.Util;

namespace RailworksForge.Views.Pages;

public partial class ConsistDetailPage : UserControl
{
    private readonly DataGridSortHandler _availableStockDataGridSortHandler;

    private readonly DataGridSortHandler _explorerStockDataGridSortHandler;

    public ConsistDetailPage()
    {
        InitializeComponent();

        _availableStockDataGridSortHandler = new(AvailableStockDataGrid);
        _explorerStockDataGridSortHandler = new(ExplorerStockDataGrid);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _availableStockDataGridSortHandler.SortColumns();
        _explorerStockDataGridSortHandler.SortColumns();
    }
}
