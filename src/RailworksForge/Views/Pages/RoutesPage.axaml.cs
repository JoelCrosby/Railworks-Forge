using Avalonia.Controls;

using RailworksForge.ViewModels;

namespace RailworksForge.Views.Pages;

public partial class RoutesPage : UserControl
{
    public RoutesPage()
    {
        InitializeComponent();
    }

    protected override void OnInitialized()
    {
        if (DataContext is not RoutesViewModel model)
        {
            return;
        }

        model.LoadRoutes();
    }
}
