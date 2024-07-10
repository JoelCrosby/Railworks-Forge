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
    public StatusBarViewModel StatusBar { get; }

    public Interaction<SaveConsistViewModel, SavedConsist?> ShowSaveConsistDialog { get; }
    public Interaction<ReplaceConsistViewModel, PreloadConsist?> ShowReplaceConsistDialog { get; }

    [ObservableProperty]
    private ViewModelBase _contentViewModel;

    private RoutesViewModel Routes { get; }

    public MainWindowViewModel(IServiceProvider provider)
    {
        Routes = provider.GetRequiredService<RoutesViewModel>();

        MainMenu = new MainMenuViewModel();
        NavigationBar = new NavigationBarViewModel();
        StatusBar = new StatusBarViewModel();

        ShowSaveConsistDialog = new Interaction<SaveConsistViewModel, SavedConsist?>();
        ShowReplaceConsistDialog = new Interaction<ReplaceConsistViewModel, PreloadConsist?>();

        _contentViewModel = Routes;
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

        ContentViewModel = Routes;
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
