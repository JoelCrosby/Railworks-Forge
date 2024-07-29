using System.Reactive;
using System.Reactive.Subjects;

using Avalonia.Controls;
using Avalonia.Input;

using RailworksForge.Controls;
using RailworksForge.ViewModels;

namespace RailworksForge.Views.Pages;

public partial class RouteDetailPage : DataGridUserControl
{
    protected override DataGrid DataGrid => ScenariosDataGrid;

    public RouteDetailPage()
    {
        InitializeComponent();
        SortColumns();
    }

    // ReSharper disable once UnusedParameter.Local
    private void DataGrid_OnDoubleTapped(object? _, TappedEventArgs args)
    {
        if (DataContext is RouteDetailViewModel context)
        {
            context.DetailsClickedCommand.Execute().Subscribe(new Subject<Unit>());
        }
    }
}
