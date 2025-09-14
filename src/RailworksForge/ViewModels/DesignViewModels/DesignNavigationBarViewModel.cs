using RailworksForge.Core;

namespace RailworksForge.ViewModels;

public class DesignNavigationBarViewModel : NavigationBarViewModel
{
    public DesignNavigationBarViewModel()
    {
        Route = new RouteDetailViewModel(new RouteViewModel(DesignData.DesignData.Route), new ScenarioService(new ScenarioDatabaseService()));
        Scenario = new ScenarioDetailViewModel(DesignData.DesignData.Scenario);
    }
}
