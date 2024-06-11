using System.Reactive;
using System.Reactive.Subjects;

using Avalonia.Controls;
using Avalonia.Input;

using RailworksForge.ViewModels;

namespace RailworksForge.Views.Controls;

public partial class RoutesList : UserControl
{
    public RoutesList()
    {
        InitializeComponent();
    }

    private void DataGrid_OnDoubleTapped(object? _, TappedEventArgs args)
    {
        if (DataContext is RoutesListViewModel context)
        {
            context.DetailsClickedCommand.Execute().Subscribe(new Subject<Unit>());
        }
    }
}
