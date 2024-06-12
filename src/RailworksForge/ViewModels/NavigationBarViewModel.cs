
using System.Reactive;

using RailworksForge.Core.Models;
using RailworksForge.Util;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public class NavigationBarViewModel : ViewModelBase
{
    private RouteDetailViewModel? _route;

    public RouteDetailViewModel? Route
    {
        get => _route;
        set
        {
            ShowRouteButton = value is not null;
            this.RaiseAndSetIfChanged(ref _route, value);
        }
    }

    private bool _showRouteButton;

    public bool ShowRouteButton
    {
        get => _showRouteButton;
        set => this.RaiseAndSetIfChanged(ref _showRouteButton, value);
    }

    private Scenario? _scenario;

    public Scenario? Scenario
    {
        get => _scenario;
        set
        {
            ShowScenarioButton = value is not null;
            this.RaiseAndSetIfChanged(ref _scenario, value);
        }
    }

    private bool _showScenarioButton;

    public bool ShowScenarioButton
    {
        get => _showScenarioButton;
        set => this.RaiseAndSetIfChanged(ref _showScenarioButton, value);
    }

    public ReactiveCommand<Unit, Unit> RoutesClickedCommand { get; } = ReactiveCommand.Create(() =>
    {
        Utils.GetApplicationViewModel().SelectAllRoutes();
    });

    public ReactiveCommand<Unit, Unit> RouteClickedCommand { get; } = ReactiveCommand.Create(() =>
    {
        Utils.GetApplicationViewModel().SelectCurrentRoute();
    });

    public ReactiveCommand<Unit, Unit> ScenarioClickedCommand { get; } = ReactiveCommand.Create(() =>
    {
        Utils.GetApplicationViewModel().SelectCurrentScenario();
    });
}
