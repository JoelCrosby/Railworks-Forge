using System;
using System.Reactive;
using System.Reactive.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.Extensions.DependencyInjection;

using RailworksForge.Core;
using RailworksForge.Core.Models;
using RailworksForge.Core.Packaging;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public MainMenuViewModel MainMenu { get; }
    public NavigationBarViewModel NavigationBar { get; }
    public StatusBarViewModel StatusBar { get; }
    public ProgressIndicatorViewModel ProgressIndicator { get; }
    private RoutesViewModel Routes { get; }

    public Interaction<SaveConsistViewModel, SavedConsist?> ShowSaveConsistDialog { get; }
    public Interaction<ReplaceConsistViewModel, PreloadConsist?> ShowReplaceConsistDialog { get; }
    public Interaction<ConfirmationDialogViewModel, bool> ShowConfirmationDialog { get; }
    public Interaction<ReplaceTrackViewModel, ReplaceTracksRequest?> ShowReplaceTrackDialog { get; }
    public Interaction<CheckAssetsViewModel, Unit?> ShowCheckAssetsDialog { get; }

    [ObservableProperty]
    private ViewModelBase _contentViewModel;

    private readonly AssetDirectoryTreeService _assetDirectoryTreeService;

    public MainWindowViewModel(IServiceProvider provider)
    {
        Routes = provider.GetRequiredService<RoutesViewModel>();

        MainMenu = provider.GetRequiredService<MainMenuViewModel>();
        NavigationBar = provider.GetRequiredService<NavigationBarViewModel>();
        StatusBar = provider.GetRequiredService<StatusBarViewModel>();
        ProgressIndicator = provider.GetRequiredService<ProgressIndicatorViewModel>();

        ShowSaveConsistDialog = new Interaction<SaveConsistViewModel, SavedConsist?>();
        ShowReplaceConsistDialog = new Interaction<ReplaceConsistViewModel, PreloadConsist?>();
        ShowConfirmationDialog = new Interaction<ConfirmationDialogViewModel, bool>();
        ShowReplaceTrackDialog = new Interaction<ReplaceTrackViewModel, ReplaceTracksRequest?>();
        ShowCheckAssetsDialog = new Interaction<CheckAssetsViewModel, Unit?>();

        _contentViewModel = Routes;
        _assetDirectoryTreeService = provider.GetRequiredService<AssetDirectoryTreeService>();
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
        NavigationBar.Consist = null;

        Routes.LoadRoutes();

        ContentViewModel = Routes;
    }

    public void SelectScenario(Scenario scenario)
    {
        var view = new ScenarioDetailViewModel(scenario);

        NavigationBar.Scenario = view;
        NavigationBar.Consist = null;

        ContentViewModel = view;
    }

    public void SelectCurrentRoute()
    {
        NavigationBar.Scenario = null;
        NavigationBar.Consist = null;

        if (NavigationBar.Route is null) return;

        SelectRoute(NavigationBar.Route.Route);
    }

    public void SelectCurrentScenario()
    {
        NavigationBar.Consist = null;

        if (NavigationBar.Scenario is null)
        {
            return;
        }

        NavigationBar.Scenario.Refresh();
        ContentViewModel = NavigationBar.Scenario;
    }

    public void SelectScenarioConsist(Scenario scenario, Consist consist)
    {
        NavigationBar.Consist = consist;

        ContentViewModel = new ConsistDetailViewModel(scenario, consist, _assetDirectoryTreeService);
    }

    public void SelectCurrentConsist()
    {
        if (NavigationBar.Scenario is null || NavigationBar.Consist is null)
        {
            return;
        }

        ContentViewModel = new ConsistDetailViewModel(NavigationBar.Scenario.Scenario, NavigationBar.Consist, _assetDirectoryTreeService);
    }

    public void UpdateProgressIndicator(InstallProgress model)
    {
        ProgressIndicator.UpdateProgress(model);
    }

    public void ClearProgressIndicator()
    {
        ProgressIndicator.ClearProgress();
    }

    public void OnLoaded()
    {
        Observable.StartAsync(_assetDirectoryTreeService.LoadDirectoryTree, RxApp.TaskpoolScheduler);
    }
}
