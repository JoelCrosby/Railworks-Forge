using System;

using Microsoft.Extensions.DependencyInjection;

using RailworksForge.Core.Models;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public MainMenuViewModel MainMenu { get; }
    public NavigationBarViewModel NavigationBar { get; }
    public RoutesListViewModel RoutesList { get; }

    private ViewModelBase _contentViewModel;

    public ViewModelBase ContentViewModel
    {
        get => _contentViewModel;
        private set => this.RaiseAndSetIfChanged(ref _contentViewModel, value);
    }

    public MainWindowViewModel(IServiceProvider provider)
    {
        RoutesList = provider.GetRequiredService<RoutesListViewModel>();

        MainMenu = new MainMenuViewModel();
        NavigationBar = new NavigationBarViewModel();

        _contentViewModel = RoutesList;
    }

    public void SelectRoute(RouteViewModel route)
    {
        NavigationBar.Route = new RouteDetailViewModel(route);
        NavigationBar.Scenario = null;

        ContentViewModel = NavigationBar.Route;
    }

    public void SelectAllRoutes()
    {
        NavigationBar.Route = null;
        NavigationBar.Scenario = null;

        ContentViewModel = RoutesList;
    }

    public void SelectScenario(Scenario scenario)
    {
        NavigationBar.Scenario = scenario;

        ContentViewModel = new ScenarioDetailViewModel(scenario);
    }

    public void SelectCurrentRoute()
    {
        NavigationBar.Scenario = null;

        ContentViewModel = NavigationBar.Route!;
    }

    public void SelectCurrentScenario()
    {
        ContentViewModel = new ScenarioDetailViewModel(NavigationBar.Scenario!);
    }

    public void SelectScenarioConsist(Scenario scenario, Consist consist)
    {
        ContentViewModel = new ConsistViewModel(scenario, consist);
    }
}
