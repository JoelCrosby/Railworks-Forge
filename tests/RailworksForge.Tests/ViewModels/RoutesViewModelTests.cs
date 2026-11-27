using NSubstitute;

using RailworksForge.Core.Routes;
using RailworksForge.Services;
using RailworksForge.Tests.Support;
using RailworksForge.ViewModels;

namespace RailworksForge.Tests.ViewModels;

public class RoutesViewModelTests
{
    private readonly IRouteService _routes = Substitute.For<IRouteService>();
    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly ILauncherService _launcher = Substitute.For<ILauncherService>();
    private readonly IClipboardService _clipboard = Substitute.For<IClipboardService>();
    private readonly IRecentRoutesService _recentRoutes = Substitute.For<IRecentRoutesService>();
    private readonly RoutesViewModel _page;

    public RoutesViewModelTests()
    {
        _routes.GetRoutes().Returns([TestData.Route("West Coast Main Line"), TestData.Route("Settle Carlisle")]);

        _page = new RoutesViewModel(
            _routes,
            _navigation,
            Substitute.For<IImageService>(),
            _launcher,
            _clipboard,
            Fakes.ImmediateDispatcher(),
            _recentRoutes);
    }

    [Fact]
    public async Task Activate_ListsTheRoutes()
    {
        await _page.Activate();

        Assert.Equal(["Settle Carlisle", "West Coast Main Line"], _page.Routes.Select(route => route.Name));
        Assert.Equal(2, _page.RouteCount);
        Assert.False(_page.IsLoading);
    }

    [Fact]
    public async Task ActivatingAgain_KeepsTheLoadedRoutes()
    {
        await _page.Activate();
        _page.Deactivate();

        await _page.Activate();

        await _routes.Received(1).GetRoutes();
    }

    [Fact]
    public async Task ActivatingAfterAnInterruptedLoad_LoadsAgain()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<List<Route>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _routes.GetRoutes().Returns(
            _ =>
            {
                entered.SetResult();

                return release.Task;
            },
            _ => Task.FromResult(new List<Route> { TestData.Route() }));
        var interrupted = _page.Activate();
        await entered.Task;
        _page.Deactivate();
        release.SetResult([]);
        await interrupted;

        await _page.Activate();

        Assert.Single(_page.Routes);
    }

    [Fact]
    public async Task InvalidatedRoutes_ReloadWhileShown()
    {
        await _page.Activate();
        _routes.GetRoutes().Returns([TestData.Route("Newly Installed")]);

        _routes.RoutesInvalidated += Raise.Event<Action>();

        await Wait.Until(() => _page.RouteCount == 1, "routes to reload");
        Assert.Equal("Newly Installed", _page.Routes[0].Name);
    }

    [Fact]
    public async Task InvalidatedRoutes_ReloadOnTheNextVisitWhenHidden()
    {
        await _page.Activate();
        _page.Deactivate();
        _routes.GetRoutes().Returns([TestData.Route("Newly Installed")]);

        _routes.RoutesInvalidated += Raise.Event<Action>();

        await _routes.Received(1).GetRoutes();

        await _page.Activate();

        Assert.Equal("Newly Installed", Assert.Single(_page.Routes).Name);
    }

    [Fact]
    public async Task SearchTerm_FiltersRoutesByName()
    {
        await _page.Activate();

        _page.SearchTerm = "SETTLE";

        Assert.Equal("Settle Carlisle", Assert.Single(_page.Routes).Name);

        _page.SearchTerm = null;

        Assert.Equal(2, _page.Routes.Count);
    }

    [Fact]
    public void LayoutCommands_SwitchBetweenGridAndList()
    {
        Assert.True(_page.IsGridLayout);

        _page.ShowListCommand.Execute(null);

        Assert.True(_page.IsListLayout);
        Assert.False(_page.IsGridLayout);

        _page.ShowGridCommand.Execute(null);

        Assert.True(_page.IsGridLayout);
    }

    [Fact]
    public async Task RouteCommands_NeedASelectedRoute()
    {
        Assert.False(_page.OpenRouteCommand.CanExecute(null));
        Assert.False(_page.CopyRouteNameCommand.CanExecute(null));
        Assert.False(_page.OpenInExplorerCommand.CanExecute(null));

        await _page.Activate();
        _page.SelectedRoute = _page.Routes[0];

        Assert.True(_page.OpenRouteCommand.CanExecute(null));
        Assert.True(_page.CopyRouteNameCommand.CanExecute(null));
        Assert.True(_page.OpenInExplorerCommand.CanExecute(null));
    }

    [Fact]
    public async Task RouteCommands_ActOnTheSelectedRoute()
    {
        await _page.Activate();
        var route = _page.Routes.First(candidate => candidate.Name == "Settle Carlisle");
        _page.SelectedRoute = route;

        _page.OpenRouteCommand.Execute(null);
        await _page.CopyRouteNameCommand.ExecuteAsync(null);
        _page.OpenInExplorerCommand.Execute(null);

        _navigation.Received(1).ShowRoute(route);
        _recentRoutes.Received(1).Record(route.Id);
        await _clipboard.Received(1).SetText("Settle Carlisle");
        _launcher.Received(1).OpenDirectory(route.DirectoryPath);
    }

    [Fact]
    public async Task RecentlyOpenedRoutes_AreListedMostRecentFirst()
    {
        _recentRoutes.GetRecentRouteIds().Returns(["West Coast Main Line", "Settle Carlisle"]);

        await _page.Activate();

        Assert.True(_page.HasRecentRoutes);
        Assert.Equal(["West Coast Main Line", "Settle Carlisle"], _page.RecentRoutes.Select(route => route.Name));
    }

    [Fact]
    public async Task SortingByRecentlyOpened_PutsRecentRoutesFirst()
    {
        _recentRoutes.GetRecentRouteIds().Returns(["West Coast Main Line"]);
        await _page.Activate();

        _page.SortOrder = RouteSortOrder.RecentlyOpened;

        Assert.Equal(["West Coast Main Line", "Settle Carlisle"], _page.Routes.Select(route => route.Name));
    }

    [Fact]
    public async Task OpeningARoute_SortedByName_KeepsTheRouteListAndRefreshesTheRecentRow()
    {
        await _page.Activate();
        var route = _page.Routes.First(candidate => candidate.Name == "West Coast Main Line");
        var resets = 0;
        _page.Routes.CollectionChanged += (_, _) => resets++;
        _recentRoutes.GetRecentRouteIds().Returns([route.Id]);
        _page.SelectedRoute = route;

        _page.OpenRouteCommand.Execute(null);

        Assert.Equal(0, resets);
        Assert.Same(route, Assert.Single(_page.RecentRoutes));
    }

    [Fact]
    public async Task OpeningARoute_SortedByRecentlyOpened_MovesItToTheFront()
    {
        await _page.Activate();
        _page.SortOrder = RouteSortOrder.RecentlyOpened;
        var route = _page.Routes.First(candidate => candidate.Name == "West Coast Main Line");
        _recentRoutes.GetRecentRouteIds().Returns([route.Id]);
        _page.SelectedRoute = route;

        _page.OpenRouteCommand.Execute(null);

        Assert.Same(route, _page.Routes[0]);
    }

    [Fact]
    public async Task DuplicateRecentRouteIds_AreListedOnce()
    {
        _recentRoutes.GetRecentRouteIds().Returns(["Settle Carlisle", "West Coast Main Line", "Settle Carlisle"]);

        await _page.Activate();
        _page.SortOrder = RouteSortOrder.RecentlyOpened;

        Assert.Equal(["Settle Carlisle", "West Coast Main Line"], _page.RecentRoutes.Select(route => route.Name));
        Assert.Equal(["Settle Carlisle", "West Coast Main Line"], _page.Routes.Select(route => route.Name));
    }

    [Fact]
    public void SortIndex_IgnoresIndexesThatAreNotASortOrder()
    {
        _page.SortIndex = (int)RouteSortOrder.RecentlyOpened;

        _page.SortIndex = -1;

        Assert.Equal(RouteSortOrder.RecentlyOpened, _page.SortOrder);
    }
}
