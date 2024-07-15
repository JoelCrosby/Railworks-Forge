using System.Reactive;
using System.Reactive.Subjects;

using Avalonia.Controls;
using Avalonia.Input;

using RailworksForge.ViewModels;

namespace RailworksForge.Views.Controls;

public partial class RoutesGrid : UserControl
{
    public RoutesGrid()
    {
        InitializeComponent();
    }

    // ReSharper disable once UnusedParameter.Local
    private void DataGrid_OnDoubleTapped(object? _, TappedEventArgs args)
    {
        if (DataContext is RoutesBaseViewModel context)
        {
            context.DetailsClickedCommand.Execute().Subscribe(new Subject<Unit>());
        }
    }
}
