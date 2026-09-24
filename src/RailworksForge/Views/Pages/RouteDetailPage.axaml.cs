using System.Reactive;
using System.Reactive.Subjects;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using RailworksForge.Util;
using RailworksForge.ViewModels;

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

    // ReSharper disable once UnusedParameter.Local
    private void ScenariosDataGrid_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (!e.IsFromDataGridRow())
        {
            return;
        }

        if (DataContext is not RouteDetailViewModel context)
        {
            return;
        }

        context.DetailsClickedCommand.Execute().Subscribe(new Subject<Unit>());
    }
}
