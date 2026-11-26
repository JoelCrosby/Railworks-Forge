using System.Threading.Tasks;

using RailworksForge.Core.Config;
using RailworksForge.Core.Scenarios;
using RailworksForge.Services;
using RailworksForge.Translations;

namespace RailworksForge.ViewModels;

public class MainWindowViewModel(
    NavigationService navigation,
    NavigationBarViewModel navigationBar,
    StatusBarViewModel statusBar,
    ToolsActivity tools,
    GameDirectoryService gameDirectory,
    ScenarioDatabaseService scenarioDatabase) : ViewModelBase
{
    public NavigationService Navigation { get; } = navigation;

    public NavigationBarViewModel NavigationBar { get; } = navigationBar;

    public StatusBarViewModel StatusBar { get; } = statusBar;

    public LoadingOperation ToolsLoading => tools.Loading;

    public ProgressIndicatorViewModel ProgressIndicator => tools.Progress;

    public void Start()
    {
        LoadingOperation.UnobservedFailure += Loading.ShowError;

        StartNavigation();
    }

    private void StartNavigation()
    {
        if (!gameDirectory.IsCurrentValid)
        {
            Navigation.ShowSettings();

            return;
        }

        Navigation.ShowRoutes();

        _ = Loading.RunAsync(Strings.loading_scenario_information.CurrentValue, async token =>
        {
            await scenarioDatabase.Start(token);

            return true;
        }, _ => { });
    }
}
