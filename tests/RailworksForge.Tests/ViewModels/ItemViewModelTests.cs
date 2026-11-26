using RailworksForge.Core.Assets;
using RailworksForge.Tests.Support;
using RailworksForge.ViewModels;

namespace RailworksForge.Tests.ViewModels;

public class ItemViewModelTests
{
    [Theory]
    [InlineData(AcquisitionState.Missing, false, RowHighlightState.Missing)]
    [InlineData(AcquisitionState.Partial, false, RowHighlightState.Partial)]
    [InlineData(AcquisitionState.Missing, true, RowHighlightState.Missing)]
    [InlineData(AcquisitionState.Found, true, RowHighlightState.Player)]
    [InlineData(AcquisitionState.Found, false, RowHighlightState.None)]
    [InlineData(AcquisitionState.Unknown, false, RowHighlightState.None)]
    public void Consist_HighlightsMissingStockBeforeThePlayer(AcquisitionState state, bool isPlayer, RowHighlightState expected)
    {
        var consist = TestData.Consist(state: state, isPlayer: isPlayer);

        var row = new ConsistViewModel(consist);

        Assert.Equal(expected, row.Highlight);
    }

    [Fact]
    public void Route_CopiesTheRouteAndIndexesItsNameForSearch()
    {
        var route = TestData.Route("West Coast Main Line");

        var row = new RouteViewModel(route);

        Assert.Same(route, row.Model);
        Assert.Equal(route.Id, row.Id);
        Assert.Equal("West Coast Main Line", row.Name);
        Assert.Equal("west coast main line", row.SearchIndex);
        Assert.Equal(route.DirectoryPath, row.DirectoryPath);
        Assert.Equal(route.RoutePropertiesPath, row.RoutePropertiesPath);
        Assert.Equal(route.PackagingType, row.PackagingType);
    }

    [Fact]
    public void ScenarioRow_RefreshPlayerInfo_PicksUpTheScenariosLatestInfo()
    {
        var scenario = TestData.Scenario();
        var row = new ScenarioRowViewModel(scenario);
        var changed = new List<string?>();
        row.PropertyChanged += (_, args) => changed.Add(args.PropertyName);
        var updated = TestData.PlayerInfo(score: 90);

        scenario.SetPlayerInfo(updated);
        row.RefreshPlayerInfo();

        Assert.Same(updated, row.PlayerInfo);
        Assert.Contains(nameof(ScenarioRowViewModel.PlayerInfo), changed);
    }
}
