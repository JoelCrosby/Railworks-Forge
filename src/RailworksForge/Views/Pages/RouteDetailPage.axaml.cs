using Avalonia.Controls;
using Avalonia.Interactivity;

using RailworksForge.Util;

namespace RailworksForge.Views.Pages;

public partial class RouteDetailPage : UserControl
{
    private readonly DataGridSortHandler _scenariosDataGridSortHandler;

    public RouteDetailPage()
    {
        InitializeComponent();

        _scenariosDataGridSortHandler = new (ScenariosDataGrid);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _scenariosDataGridSortHandler.SortColumns();
    }
}
