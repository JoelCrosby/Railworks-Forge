using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.Extensions.DependencyInjection;

using RailworksForge.Core;
using RailworksForge.Core.Models;
using RailworksForge.Core.Packaging;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public ToolbarViewModel Toolbar { get; }
    public NavigationBarViewModel NavigationBar { get; }
    public StatusBarViewModel StatusBar { get; }
    public ProgressIndicatorViewModel ProgressIndicator { get; }

    public LoadingOperation ToolsLoading { get; } = new();
    private RoutesViewModel Routes { get; }

    public Interaction<SaveConsistViewModel, SavedConsist?> ShowSaveConsistDialog { get; }
    public Interaction<ReplaceConsistViewModel, PreloadConsist?> ShowReplaceConsistDialog { get; }
    public Interaction<ConfirmationDialogViewModel, bool> ShowConfirmationDialog { get; }
    public Interaction<ReplaceTrackViewModel, ReplaceTracksRequest?> ShowReplaceTrackDialog { get; }
    public Interaction<CheckAssetsViewModel, Unit?> ShowCheckAssetsDialog { get; }

    [ObservableProperty]
    private ViewModelBase _contentViewModel;

    private readonly AssetDirectoryTreeService _assetDirectoryTreeService;
    private readonly ScenarioDatabaseService _scenarioDatabaseService;
    private readonly ScenarioService _scenarioService;
    private readonly IServiceProvider _provider;

    public MainWindowViewModel(IServiceProvider provider)
    {
        Routes = provider.GetRequiredService<RoutesViewModel>();
        NavigationBar = provider.GetRequiredService<NavigationBarViewModel>();
        StatusBar = provider.GetRequiredService<StatusBarViewModel>();
        ProgressIndicator = provider.GetRequiredService<ProgressIndicatorViewModel>();
        Toolbar = provider.GetRequiredService<ToolbarViewModel>();

        ShowSaveConsistDialog = new Interaction<SaveConsistViewModel, SavedConsist?>();
        ShowReplaceConsistDialog = new Interaction<ReplaceConsistViewModel, PreloadConsist?>();
        ShowConfirmationDialog = new Interaction<ConfirmationDialogViewModel, bool>();
        ShowReplaceTrackDialog = new Interaction<ReplaceTrackViewModel, ReplaceTracksRequest?>();
        ShowCheckAssetsDialog = new Interaction<CheckAssetsViewModel, Unit?>();

        _contentViewModel = Routes;

        _assetDirectoryTreeService = provider.GetRequiredService<AssetDirectoryTreeService>();
        _scenarioDatabaseService = provider.GetRequiredService<ScenarioDatabaseService>();
        _scenarioService = provider.GetRequiredService<ScenarioService>();
        _provider = provider;
    }

    public void SelectRoute(RouteViewModel route)
    {
        NavigationBar.Route = new RouteDetailViewModel(route, _scenarioService);
        NavigationBar.Scenario = null;

        ContentViewModel = NavigationBar.Route;
    }

    public void SelectAllRoutes()
    {
        NavigationBar.Route = null;
        NavigationBar.Scenario = null;
        NavigationBar.Consist = null;

        ContentViewModel = Routes;
        Routes.LoadRoutes();
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

        ContentViewModel = NavigationBar.Scenario;
        NavigationBar.Scenario.Refresh();
    }

    public void SelectScenarioConsist(Scenario scenario, Consist consist)
    {
        var view = new ConsistDetailViewModel(scenario, consist, _assetDirectoryTreeService);

        NavigationBar.Consist = consist;

        ContentViewModel = view;
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
        Avalonia.Threading.Dispatcher.UIThread.Post(() => ProgressIndicator.UpdateProgress(model));
    }

    public void ClearProgressIndicator()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(ProgressIndicator.ClearProgress);
    }

    partial void OnContentViewModelChanging(ViewModelBase value)
    {

        if (!ReferenceEquals(ContentViewModel, value))
        {
            ContentViewModel?.CancelLoading();
        }
    }

    partial void OnContentViewModelChanged(ViewModelBase value)
    {
        NavigationBar.IsSettingsActive = value is SettingsViewModel;
        value.Activate();
    }

    public void SelectSettings()
    {

        if (ContentViewModel is SettingsViewModel)
        {
            return;
        }

        ContentViewModel = _provider.GetRequiredService<SettingsViewModel>();
    }

    public void OnLoaded()
    {

        if (!Paths.IsValidGameDirectory(Paths.GetGameDirectory()))
        {
            SelectSettings();

            return;
        }

        _ = Loading.RunAsync("Loading scenario information…", async _ =>
        {
            await _scenarioDatabaseService.LoadScenarioDatabase();
            _scenarioDatabaseService.WatchForChanges();

            return true;
        }, _ => { });
    }
}
