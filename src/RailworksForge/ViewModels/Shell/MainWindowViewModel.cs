using System.Threading.Tasks;

using RailworksForge.Core;
using RailworksForge.Services;

namespace RailworksForge.ViewModels;

public class MainWindowViewModel(
    NavigationService navigation,
    NavigationBarViewModel navigationBar,
    StatusBarViewModel statusBar,
    ToolsActivity tools,
    ScenarioDatabaseService scenarioDatabase) : ViewModelBase
{
    public NavigationService Navigation { get; } = navigation;

    public NavigationBarViewModel NavigationBar { get; } = navigationBar;

    public StatusBarViewModel StatusBar { get; } = statusBar;

    public LoadingOperation ToolsLoading => tools.Loading;

    public ProgressIndicatorViewModel ProgressIndicator => tools.Progress;

    public void Start()
    {
        var hasValidGameDirectory = Paths.IsValidGameDirectory(Paths.GetGameDirectory());

        if (!hasValidGameDirectory)
        {
            Navigation.ShowSettings();

            return;
        }

        Navigation.ShowRoutes();

        _ = Loading.RunAsync("Loading scenario information…", async _ =>
        {
            await scenarioDatabase.LoadScenarioDatabase();
            scenarioDatabase.WatchForChanges();

            return true;
        }, _ => { });
    }
}
