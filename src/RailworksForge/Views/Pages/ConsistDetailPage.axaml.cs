using System.Linq;

using Avalonia.Controls;

using RailworksForge.Core.Models;
using RailworksForge.ViewModels;

namespace RailworksForge.Views.Pages;

public partial class ConsistDetailPage : UserControl
{
    public ConsistDetailPage()
    {
        InitializeComponent();
    }

    // ReSharper disable once UnusedParameter.Local
    private void DataGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not DataGrid dataGrid) return;
        if (DataContext is not ConsistDetailViewModel context) return;

        if (dataGrid.SelectedItems is null) return;

        context.SelectedConsistVehicles = dataGrid.SelectedItems.Cast<ConsistRailVehicle>().ToList();
    }
}
