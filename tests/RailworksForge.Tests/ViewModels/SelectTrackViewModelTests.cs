using NSubstitute;

using RailworksForge.Core.Routes;
using RailworksForge.Tests.Support;
using RailworksForge.ViewModels;

namespace RailworksForge.Tests.ViewModels;

public class SelectTrackViewModelTests
{
    private readonly ITrackService _tracks = Substitute.For<ITrackService>();
    private readonly SelectTrackViewModel _selectTrack;

    public SelectTrackViewModelTests()
    {
        _selectTrack = new SelectTrackViewModel(_tracks)
        {
            Providers = ["DTG", "Armstrong Powerhouse"],
            RouteBlueprint = TestData.Blueprint(@"RailNetwork\Track\Old.xml"),
            TrackCount = 12,
        };
    }

    [Fact]
    public async Task SelectingAProvider_LoadsItsProducts()
    {
        _tracks.GetProducts("DTG").Returns(["WCML", "ECML"]);

        _selectTrack.SelectedProvider = "DTG";

        await Wait.Until(() => _selectTrack.Products.Count == 2, "products to load");
        Assert.Equal(["WCML", "ECML"], _selectTrack.Products);
    }

    [Fact]
    public async Task SelectingAProduct_LoadsItsTracks()
    {
        var track = TestData.Track();
        _tracks.GetProducts("DTG").Returns(["WCML"]);
        _tracks.GetTracks("DTG", "WCML", Arg.Any<CancellationToken>()).Returns([track]);
        _selectTrack.SelectedProvider = "DTG";
        await Wait.Until(() => _selectTrack.Products.Count == 1, "products to load");

        _selectTrack.SelectedProduct = "WCML";

        await Wait.Until(() => _selectTrack.Tracks.Count == 1, "tracks to load");
        Assert.Same(track, _selectTrack.Tracks[0]);
    }

    [Fact]
    public async Task ChangingProvider_ClearsTheProductAndTrackChoices()
    {
        _tracks.GetProducts(Arg.Any<string>()).Returns(["WCML"]);
        _tracks.GetTracks("DTG", "WCML", Arg.Any<CancellationToken>()).Returns([TestData.Track()]);
        _selectTrack.SelectedProvider = "DTG";
        await Wait.Until(() => _selectTrack.Products.Count == 1, "products to load");
        _selectTrack.SelectedProduct = "WCML";
        await Wait.Until(() => _selectTrack.Tracks.Count == 1, "tracks to load");
        _selectTrack.SelectedTrack = _selectTrack.Tracks[0];

        _selectTrack.SelectedProvider = "Armstrong Powerhouse";

        Assert.Null(_selectTrack.SelectedProduct);
        Assert.Null(_selectTrack.SelectedTrack);
        Assert.Empty(_selectTrack.Tracks);
    }

    [Fact]
    public void ClearingTheProvider_LoadsNothing()
    {
        _selectTrack.SelectedProvider = "DTG";
        _selectTrack.SelectedProvider = null;

        Assert.Empty(_selectTrack.Products);
        Assert.False(_selectTrack.Loading.IsLoading);
    }
}
