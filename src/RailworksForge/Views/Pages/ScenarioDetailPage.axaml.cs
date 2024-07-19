using System;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Input;

using RailworksForge.Core.Models;
using RailworksForge.ViewModels;

namespace RailworksForge.Views.Pages;

public partial class ScenarioDetailPage : UserControl
{
    public ScenarioDetailPage()
    {
        InitializeComponent();
    }

    // ReSharper disable once UnusedParameter.Local
    private void DataGrid_OnDoubleTapped(object? _, TappedEventArgs args)
    {
        if (args.Source is not Border) return;

        if (DataContext is ScenarioDetailViewModel context)
        {
            context.ClickedConsistCommand.Execute().Subscribe();
        }
    }

    // ReSharper disable once UnusedParameter.Local
    private void DataGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not DataGrid dataGrid) return;
        if (DataContext is not ScenarioDetailViewModel context) return;

        if (dataGrid.SelectedItems is null) return;

        context.SelectedConsists = dataGrid.SelectedItems.Cast<Consist>();
    }
}
