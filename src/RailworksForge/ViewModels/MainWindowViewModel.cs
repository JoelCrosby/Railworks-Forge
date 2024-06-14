using System;

using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.Extensions.DependencyInjection;

using RailworksForge.Core.Models;

namespace RailworksForge.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public MainMenuViewModel MainMenu { get; }
    public NavigationBarViewModel NavigationBar { get; }
    public RoutesListViewModel RoutesList { get; }
    public StatusBarViewModel StatusBar { get; }

    [ObservableProperty]
    private ViewModelBase _contentViewModel;

    public MainWindowViewModel(IServiceProvider provider)
    {
        RoutesList = provider.GetRequiredService<RoutesListViewModel>();

        MainMenu = new MainMenuViewModel();
        NavigationBar = new NavigationBarViewModel();
        StatusBar = new StatusBarViewModel();

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
        ContentViewModel = new ConsistDetailViewModel(scenario, consist);
    }
}
