using System.Reactive;
using System.Reactive.Subjects;

using Avalonia.Controls;
using Avalonia.Input;

using RailworksForge.ViewModels;

namespace RailworksForge.Views.Controls;

public partial class RouteDetail : UserControl
{
    public RouteDetail()
    {
        InitializeComponent();
    }

    // ReSharper disable once UnusedParameter.Local
    private void DataGrid_OnDoubleTapped(object? _, TappedEventArgs args)
    {
        if (args.Source is not Border) return;

        if (DataContext is RouteDetailViewModel context)
        {
            context.DetailsClickedCommand.Execute().Subscribe(new Subject<Unit>());
        }
    }
}
