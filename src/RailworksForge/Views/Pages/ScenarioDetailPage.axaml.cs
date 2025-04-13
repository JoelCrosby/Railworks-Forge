using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Subjects;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using RailworksForge.Controls;
using RailworksForge.ViewModels;

namespace RailworksForge.Views.Pages;

public partial class ScenarioDetailPage : TreeDataGridUserControl
{
    protected override TreeDataGrid DataGrid => ServicesDataGrid;

    public ScenarioDetailPage()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        SortColumns();
    }

    // ReSharper disable once UnusedParameter.Local
    private void ServicesDataGrid_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not ScenarioDetailViewModel context) return;
        if (ServicesDataGrid.RowSelection?.SelectedItems is not IReadOnlyList<ConsistViewModel> items) return;

        context.SelectedItems = items;
        context.ClickedConsistCommand.Execute().Subscribe(new Subject<Unit>());
    }

    private void MenuBase_OnOpened(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ScenarioDetailViewModel context) return;

        if (DataGrid.RowSelection?.SelectedItem is ConsistViewModel consist)
        {
            context.SelectedItems = [consist];
        }
    }
}
