
using RailworksForge.Core;

namespace RailworksForge.ViewModels;

public class DesignRouteDetailViewModel : RouteDetailViewModel
{
    public DesignRouteDetailViewModel() : base(new RouteViewModel(DesignData.DesignData.Route), new ScenarioService(new ScenarioDatabaseService()))
    {

    }
}
