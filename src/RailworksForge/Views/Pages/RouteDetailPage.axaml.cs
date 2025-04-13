using System.Reactive;
using System.Reactive.Subjects;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using RailworksForge.Controls;
using RailworksForge.Core.Models;
using RailworksForge.ViewModels;

namespace RailworksForge.Views.Pages;

public partial class RouteDetailPage : TreeDataGridUserControl
{
    protected override TreeDataGrid DataGrid => ScenariosDataGrid;

    public RouteDetailPage()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        SortColumns();
    }

    // ReSharper disable once UnusedParameter.Local
    private void ScenariosDataGrid_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not RouteDetailViewModel context) return;
        if (ScenariosDataGrid.RowSelection?.SelectedItem is not Scenario scenario) return;

        context.SelectedItem = scenario;
        context.DetailsClickedCommand.Execute().Subscribe(new Subject<Unit>());
    }

    private void MenuBase_OnOpened(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not RouteDetailViewModel context) return;

        context.SelectedItem = DataGrid.RowSelection?.SelectedItem as Scenario;
    }
}
