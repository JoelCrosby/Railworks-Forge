using NSubstitute;

using RailworksForge.Core.Routes;
using RailworksForge.Core.Scenarios;
using RailworksForge.Services;
using RailworksForge.Tests.Support;
using RailworksForge.ViewModels;

namespace RailworksForge.Tests.ViewModels;

public class RouteDetailViewModelTests
{
    private readonly RouteViewModel _route = new(TestData.Route());
    private readonly IScenarioService _scenarios = Substitute.For<IScenarioService>();
    private readonly ITrackService _tracks = Substitute.For<ITrackService>();
    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();
    private readonly ILauncherService _launcher = Substitute.For<ILauncherService>();
    private readonly IClipboardService _clipboard = Substitute.For<IClipboardService>();
    private readonly Scenario _first = TestData.Scenario("1A01 Euston - Glasgow");
    private readonly Scenario _second = TestData.Scenario("1S96 Willesden - Shieldmuir");
    private readonly RouteDetailViewModel _page;

    public RouteDetailViewModelTests()
    {
        _scenarios.GetScenarios(_route.Model, Arg.Any<CancellationToken>()).Returns([_first, _second]);

        _page = new RouteDetailViewModel(
            _route,
            _scenarios,
            _tracks,
            Substitute.For<IImageService>(),
            _navigation,
            _dialogs,
            _launcher,
            _clipboard,
            Fakes.ImmediateDispatcher());
    }

    [Fact]
    public async Task Activate_ListsTheRoutesScenariosWithPlayerInfo()
    {
        await _page.Activate();

        Assert.Equal([_first, _second], _page.Scenarios.Select(row => row.Scenario));
        Assert.Equal(2, _page.ScenarioCount);
        _scenarios.Received(1).RefreshPlayerInfo(Arg.Is<IEnumerable<Scenario>>(scenarios => scenarios.Count() == 2));
    }

    [Fact]
    public async Task SearchTerm_FiltersScenarios()
    {
        await _page.Activate();

        _page.SearchTerm = "willesden";

        Assert.Same(_second, Assert.Single(_page.Scenarios).Scenario);
    }

    [Fact]
    public async Task PlayerInfoUpdate_RefreshesTheRowsWhileShown()
    {
        await _page.Activate();
        var updated = TestData.PlayerInfo(score: 75);
        _scenarios
            .When(service => service.RefreshPlayerInfo(Arg.Any<IEnumerable<Scenario>>()))
            .Do(call => call.Arg<IEnumerable<Scenario>>().ToList().ForEach(scenario => scenario.SetPlayerInfo(updated)));

        _scenarios.PlayerInfoUpdated += Raise.Event<Action>();

        Assert.All(_page.Scenarios, row => Assert.Same(updated, row.PlayerInfo));
    }

    [Fact]
    public async Task Deactivate_StopsListeningForPlayerInfo()
    {
        await _page.Activate();
        _page.Deactivate();
        _scenarios.ClearReceivedCalls();

        _scenarios.PlayerInfoUpdated += Raise.Event<Action>();

        _scenarios.DidNotReceiveWithAnyArgs().RefreshPlayerInfo(default!);
    }

    [Fact]
    public async Task ActivatingTwice_RefreshesPlayerInfoOncePerUpdate()
    {
        await _page.Activate();
        await _page.Activate();
        _scenarios.ClearReceivedCalls();

        _scenarios.PlayerInfoUpdated += Raise.Event<Action>();

        _scenarios.ReceivedWithAnyArgs(1).RefreshPlayerInfo(default!);
    }

    [Fact]
    public async Task ScenarioCommands_NeedASelectedScenario()
    {
        Assert.False(_page.OpenScenarioCommand.CanExecute(null));
        Assert.False(_page.CopyScenarioNameCommand.CanExecute(null));
        Assert.False(_page.OpenScenarioInExplorerCommand.CanExecute(null));

        await _page.Activate();
        _page.SelectedScenario = _page.Scenarios[0];

        Assert.True(_page.OpenScenarioCommand.CanExecute(null));
        Assert.True(_page.CopyScenarioNameCommand.CanExecute(null));
        Assert.True(_page.OpenScenarioInExplorerCommand.CanExecute(null));
    }

    [Fact]
    public async Task ScenarioCommands_ActOnTheSelectedScenario()
    {
        await _page.Activate();
        _page.SelectedScenario = _page.Scenarios[1];

        _page.OpenScenarioCommand.Execute(null);
        await _page.CopyScenarioNameCommand.ExecuteAsync(null);
        _page.OpenScenarioInExplorerCommand.Execute(null);

        _navigation.Received(1).ShowScenario(_second);
        await _clipboard.Received(1).SetText(_second.Name);
        _launcher.Received(1).OpenDirectory(_second.BrowsableDirectoryPath);
    }

    [Fact]
    public void OpenInExplorer_OpensTheRouteDirectory()
    {
        _page.OpenInExplorerCommand.Execute(null);

        _launcher.Received(1).OpenDirectory(_route.DirectoryPath);
    }

    [Fact]
    public async Task ReplaceTrack_ReplacesTheChosenTracks()
    {
        var request = new ReplaceTracksRequest { Replacements = [] };
        var dialog = new ReplaceTrackViewModel(_route.Model, _tracks);
        _dialogs.Create<ReplaceTrackViewModel>(Arg.Any<object[]>()).Returns(dialog);
        _dialogs.Show(dialog).Returns(request);

        await _page.ReplaceTrackCommand.ExecuteAsync(null);

        await _tracks.Received(1).ReplaceTracks(_route.Model, request);
    }

    [Fact]
    public async Task ReplaceTrack_CancelledDialog_ReplacesNothing()
    {
        var dialog = new ReplaceTrackViewModel(_route.Model, _tracks);
        _dialogs.Create<ReplaceTrackViewModel>(Arg.Any<object[]>()).Returns(dialog);
        _dialogs.Show(dialog).Returns((ReplaceTracksRequest?)null);

        await _page.ReplaceTrackCommand.ExecuteAsync(null);

        await _tracks.DidNotReceiveWithAnyArgs().ReplaceTracks(default!, default!);
    }

    [Fact]
    public async Task CheckAssets_OpensTheAssetCheckForThisRoute()
    {
        var dialog = new CheckAssetsViewModel(_route.Model, Substitute.For<IRouteAssetCheckService>());
        _dialogs.Create<CheckAssetsViewModel>(_route.Model).Returns(dialog);

        await _page.CheckAssetsCommand.ExecuteAsync(null);

        await _dialogs.Received(1).Show(dialog);
    }
}
