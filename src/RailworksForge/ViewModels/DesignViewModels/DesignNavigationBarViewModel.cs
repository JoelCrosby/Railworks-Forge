using RailworksForge.Core.Models.Examples;

namespace RailworksForge.ViewModels;

public class DesignNavigationBarViewModel : NavigationBarViewModel
{
    public DesignNavigationBarViewModel()
    {
        Route = new RouteViewModel(Example.Route);
        Scenario = Example.Scenario;
    }
}
