using System;

using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.Extensions.DependencyInjection;

using RailworksForge.Core.Models;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public MainMenuViewModel MainMenu { get; }
    public NavigationBarViewModel NavigationBar { get; }
    public RoutesListViewModel RoutesList { get; }
    public StatusBarViewModel StatusBar { get; }

    public Interaction<SaveConsistViewModel, SaveConsistViewModel?> ShowSaveConsistDialog { get; }
    public Interaction<ReplaceConsistViewModel, SavedConsist?> ShowReplaceConsistDialog { get; }

    [ObservableProperty]
    private ViewModelBase _contentViewModel;

    public MainWindowViewModel(IServiceProvider provider)
    {
        RoutesList = provider.GetRequiredService<RoutesListViewModel>();

        MainMenu = new MainMenuViewModel();
        NavigationBar = new NavigationBarViewModel();
        StatusBar = new StatusBarViewModel();

        ShowSaveConsistDialog = new Interaction<SaveConsistViewModel, SaveConsistViewModel?>();
        ShowReplaceConsistDialog = new Interaction<ReplaceConsistViewModel, SavedConsist?>();

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
        NavigationBar.Consist = null;

        ContentViewModel = new ScenarioDetailViewModel(scenario);
    }

    public void SelectCurrentRoute()
    {
        NavigationBar.Scenario = null;
        NavigationBar.Consist = null;

        ContentViewModel = NavigationBar.Route!;
    }

    public void SelectCurrentScenario()
    {
        NavigationBar.Consist = null;

        if (NavigationBar.Scenario is null)
        {
            return;
        }

        ContentViewModel = new ScenarioDetailViewModel(NavigationBar.Scenario!);
    }

    public void SelectScenarioConsist(Scenario scenario, Consist consist)
    {
        NavigationBar.Consist = consist;

        ContentViewModel = new ConsistDetailViewModel(scenario, consist);
    }

    public void SelectCurrentConsist()
    {
        if (NavigationBar.Scenario is null || NavigationBar.Consist is null)
        {
            return;
        }

        ContentViewModel = new ConsistDetailViewModel(NavigationBar.Scenario, NavigationBar.Consist);
    }
}
