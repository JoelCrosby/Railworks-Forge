namespace RailworksForge.ViewModels;

public class DesignNavigationBarViewModel : NavigationBarViewModel
{
    public DesignNavigationBarViewModel()
    {
        Route = new RouteDetailViewModel(new RouteViewModel(DesignData.DesignData.Route));
        Scenario = new ScenarioDetailViewModel(DesignData.DesignData.Scenario);
    }
}
