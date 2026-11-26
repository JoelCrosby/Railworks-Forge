using NSubstitute;

using RailworksForge.Core.Routes;
using RailworksForge.Tests.Support;
using RailworksForge.ViewModels;

namespace RailworksForge.Tests.ViewModels;

public class ReplaceTrackViewModelTests
{
    private readonly ITrackService _tracks = Substitute.For<ITrackService>();
    private readonly ReplaceTrackViewModel _dialog;

    public ReplaceTrackViewModelTests()
    {
        _dialog = new ReplaceTrackViewModel(TestData.Route(), _tracks);
    }

    [Fact]
    public async Task Activate_ListsEachRouteTrackWithTheAvailableProviders()
    {
        var concrete = new TrackBlueprint(TestData.Blueprint(@"RailNetwork\Track\Concrete.xml"));
        var timber = new TrackBlueprint(TestData.Blueprint(@"RailNetwork\Track\Timber.xml"));
        timber.Add();
        GivenRouteTracks(concrete, timber);

        await _dialog.Activate();

        Assert.Equal(2, _dialog.RouteTracks.Count);
        Assert.Same(concrete.Blueprint, _dialog.RouteTracks[0].RouteBlueprint);
        Assert.Equal(2, _dialog.RouteTracks[1].TrackCount);
        Assert.Equal(["DTG"], _dialog.RouteTracks[0].Providers);
    }

    [Fact]
    public async Task ReplaceTracks_IsDisabledUntilAReplacementIsChosen()
    {
        GivenRouteTracks(new TrackBlueprint(TestData.Blueprint()));
        await _dialog.Activate();

        Assert.False(_dialog.ReplaceTracksCommand.CanExecute(null));

        _dialog.RouteTracks[0].SelectedTrack = TestData.Track();

        Assert.True(_dialog.ReplaceTracksCommand.CanExecute(null));
    }

    [Fact]
    public async Task ReplaceTracks_ClosesWithEveryRouteTrackAndItsReplacement()
    {
        var concrete = new TrackBlueprint(TestData.Blueprint(@"RailNetwork\Track\Concrete.xml"));
        var timber = new TrackBlueprint(TestData.Blueprint(@"RailNetwork\Track\Timber.xml"));
        var replacement = TestData.Track();
        GivenRouteTracks(concrete, timber);
        await _dialog.Activate();
        _dialog.RouteTracks[0].SelectedTrack = replacement;
        object? result = null;
        _dialog.CloseRequested += value => result = value;

        _dialog.ReplaceTracksCommand.Execute(null);

        var request = Assert.IsType<ReplaceTracksRequest>(result);
        Assert.Equal(2, request.Replacements.Count);
        Assert.Same(replacement.Blueprint, request.Replacements[0].ReplacementBlueprint);
        Assert.Same(timber.Blueprint, request.Replacements[1].Blueprint);
        Assert.Null(request.Replacements[1].ReplacementBlueprint);
    }

    [Fact]
    public async Task ReplaceTracks_IsDisabledWhileATrackIsStillLoading()
    {
        var release = new TaskCompletionSource<List<string>>();
        GivenRouteTracks(new TrackBlueprint(TestData.Blueprint()), new TrackBlueprint(TestData.Blueprint()));
        _tracks.GetProducts("DTG").Returns(_ => release.Task.Result);
        await _dialog.Activate();
        _dialog.RouteTracks[0].SelectedTrack = TestData.Track();

        _dialog.RouteTracks[1].SelectedProvider = "DTG";

        Assert.False(_dialog.ReplaceTracksCommand.CanExecute(null));

        release.SetResult(["WCML"]);
        await Wait.Until(() => !_dialog.RouteTracks[1].Loading.IsLoading, "the product load to finish");

        Assert.True(_dialog.ReplaceTracksCommand.CanExecute(null));
    }

    [Fact]
    public async Task ReloadingRouteTracks_StopsThePreviousRowsLoading()
    {
        var release = new TaskCompletionSource<List<string>>();
        GivenRouteTracks(new TrackBlueprint(TestData.Blueprint()));
        _tracks.GetProducts("DTG").Returns(_ => release.Task.Result);
        await _dialog.Activate();
        var previousRow = _dialog.RouteTracks[0];
        previousRow.SelectedProvider = "DTG";

        await _dialog.Activate();

        Assert.NotSame(previousRow, _dialog.RouteTracks[0]);
        Assert.False(previousRow.Loading.IsLoading);
        release.SetResult([]);
    }

    [Fact]
    public async Task Deactivate_StopsEveryRowLoading()
    {
        var release = new TaskCompletionSource<List<string>>();
        GivenRouteTracks(new TrackBlueprint(TestData.Blueprint()));
        _tracks.GetProducts("DTG").Returns(_ => release.Task.Result);
        await _dialog.Activate();
        _dialog.RouteTracks[0].SelectedProvider = "DTG";

        _dialog.Deactivate();

        Assert.False(_dialog.RouteTracks[0].Loading.IsLoading);
        release.SetResult([]);
    }

    private void GivenRouteTracks(params TrackBlueprint[] blueprints)
    {
        _tracks.GetRouteTrackBlueprints(Arg.Any<Route>()).Returns([.. blueprints]);
        _tracks.GetProviders().Returns(["DTG"]);
    }
}
