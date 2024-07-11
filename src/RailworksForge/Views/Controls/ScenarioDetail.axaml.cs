using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;

using Avalonia.Controls;
using Avalonia.Input;

using RailworksForge.Core.Models;
using RailworksForge.ViewModels;

namespace RailworksForge.Views.Controls;

public partial class ScenarioDetail : UserControl
{
    public ScenarioDetail()
    {
        InitializeComponent();
    }

    // ReSharper disable once UnusedParameter.Local
    private void DataGrid_OnDoubleTapped(object? _, TappedEventArgs args)
    {
        if (DataContext is ScenarioDetailViewModel context)
        {
            context.ClickedConsistCommand.Execute().Subscribe(new Subject<Unit>());
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
