using Avalonia.Controls;

using RailworksForge.ViewModels;

namespace RailworksForge.Views.Pages;

public partial class RoutesPage : UserControl
{
    public RoutesPage()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (DataContext is not RoutesViewModel model)
        {
            return;
        }

        model.LoadRoutes();
    }
}
