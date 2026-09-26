using Avalonia.Controls;
using Avalonia.Interactivity;

using RailworksForge.Util;

namespace RailworksForge.Views.Pages;

public partial class ScenarioDetailPage : UserControl
{
    private readonly DataGridSortHandler _servicesDataGridSortHandler;

    public ScenarioDetailPage()
    {
        InitializeComponent();

        _servicesDataGridSortHandler  = new (ServicesDataGrid);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _servicesDataGridSortHandler.SortColumns();
    }
}
