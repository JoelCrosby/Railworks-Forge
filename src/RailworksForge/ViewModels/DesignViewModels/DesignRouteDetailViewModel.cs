using System.Collections.ObjectModel;

using RailworksForge.Core.Models;
using RailworksForge.Core.Models.Examples;

namespace RailworksForge.ViewModels;

public class DesignRouteDetailViewModel : RouteDetailViewModel
{
    public override ObservableCollection<Scenario> Scenarios { get; init; } = new(Example.Scenarios);

    public DesignRouteDetailViewModel() : base(Example.Route) { }
}
