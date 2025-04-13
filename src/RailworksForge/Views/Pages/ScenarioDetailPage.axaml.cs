using System.Reactive;
using System.Reactive.Subjects;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using RailworksForge.Util;
using RailworksForge.ViewModels;

namespace RailworksForge.Views.Pages;

public partial class ScenarioDetailPage : UserControl
{
    private readonly TreeDataGridSortHandler _servicesDataGridSortHandler;

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

    // ReSharper disable once UnusedParameter.Local
    private void ServicesDataGrid_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not ScenarioDetailViewModel context) return;

        context.ClickedConsistCommand.Execute().Subscribe(new Subject<Unit>());
    }
}
