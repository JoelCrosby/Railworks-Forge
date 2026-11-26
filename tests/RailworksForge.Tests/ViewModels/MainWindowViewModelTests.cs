using NSubstitute;

using RailworksForge.Core.Config;
using RailworksForge.Core.Packaging;
using RailworksForge.Core.Scenarios;
using RailworksForge.Core.Serialization;
using RailworksForge.Services;
using RailworksForge.Tests.Support;
using RailworksForge.ViewModels;

namespace RailworksForge.Tests.ViewModels;

[Collection(GlobalStateCollection.Name)]
public class MainWindowViewModelTests
{
    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly IGameDirectoryService _gameDirectory = Substitute.For<IGameDirectoryService>();
    private readonly IScenarioDatabaseService _scenarioDatabase = Substitute.For<IScenarioDatabaseService>();
    private readonly ToolsActivity _tools = new();
    private readonly MainWindowViewModel _mainWindow;

    public MainWindowViewModelTests()
    {
        var picker = Substitute.For<IStoragePickerService>();
        var serz = Substitute.For<ISerzFileService>();
        var packages = Substitute.For<IPackageInstallService>();
        var toolsMenu = new ToolsMenuViewModel(_tools, picker, serz, packages);
        var navigationBar = new NavigationBarViewModel(_navigation, toolsMenu, Substitute.For<IDialogService>());
        var statusBar = new StatusBarViewModel(_gameDirectory);

        _mainWindow = new MainWindowViewModel(_navigation, navigationBar, statusBar, _tools, _gameDirectory, _scenarioDatabase);
    }

    [Fact]
    public void Start_WithoutAValidGameDirectory_OpensSettings()
    {
        _gameDirectory.IsCurrentValid.Returns(false);

        _mainWindow.Start();

        _navigation.Received(1).ShowSettings();
        _navigation.DidNotReceive().ShowRoutes();
        _scenarioDatabase.DidNotReceive().Start(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Start_WithAValidGameDirectory_ShowsRoutesAndLoadsScenarioDatabase()
    {
        _gameDirectory.IsCurrentValid.Returns(true);

        _mainWindow.Start();

        _navigation.Received(1).ShowRoutes();
        await Wait.Until(() => _scenarioDatabase.ReceivedCalls().Any(), "the scenario database to start");
        await _scenarioDatabase.Received(1).Start(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Start_ScenarioDatabaseFailure_IsShownOnTheMainWindow()
    {
        _gameDirectory.IsCurrentValid.Returns(true);
        _scenarioDatabase.Start(Arg.Any<CancellationToken>()).Returns(Task.FromException(new IOException("Database locked")));

        _mainWindow.Start();

        await Wait.Until(() => _mainWindow.Loading.HasError, "the failure to show");
        Assert.Equal("Database locked", _mainWindow.Loading.ErrorMessage);
    }

    [Fact]
    public void UnobservedFailure_AfterStart_IsShownOnTheMainWindow()
    {
        _gameDirectory.IsCurrentValid.Returns(false);
        _mainWindow.Start();

        LoadingOperation.ReportUnobservedFailure("Saving: Disk full");

        Assert.Equal("Saving: Disk full", _mainWindow.Loading.ErrorMessage);
    }

    [Fact]
    public void ToolsState_IsSharedWithTheToolsMenu()
    {
        Assert.Same(_tools.Loading, _mainWindow.ToolsLoading);
        Assert.Same(_tools.Progress, _mainWindow.ProgressIndicator);
    }
}
