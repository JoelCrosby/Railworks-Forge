using RailworksForge.Core.Models.Examples;

namespace RailworksForge.ViewModels;

public class DesignNavigationBarViewModel : NavigationBarViewModel
{
    public DesignNavigationBarViewModel()
    {
        Route = new RouteDetailViewModel(new RouteViewModel(Example.Route));
        Scenario = Example.Scenario;
    }
}
