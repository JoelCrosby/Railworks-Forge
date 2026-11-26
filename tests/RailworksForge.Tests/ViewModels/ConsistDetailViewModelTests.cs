using System.Collections.ObjectModel;

using NSubstitute;

using RailworksForge.Core.Assets;
using RailworksForge.Core.Consists;
using RailworksForge.Core.Scenarios;
using RailworksForge.Core.Vehicles;
using RailworksForge.Services;
using RailworksForge.Tests.Support;
using RailworksForge.Translations;
using RailworksForge.ViewModels;

namespace RailworksForge.Tests.ViewModels;

[Collection(GlobalStateCollection.Name)]
public class ConsistDetailViewModelTests
{
    private readonly Consist _consist = TestData.Consist();
    private readonly IScenarioEditSession _session = Substitute.For<IScenarioEditSession>();
    private readonly IScenarioEditor _editor;
    private readonly IVehicleIndex _index = Substitute.For<IVehicleIndex>();
    private readonly ILauncherService _launcher = Substitute.For<ILauncherService>();
    private readonly IAssetDirectoryTreeService _directoryTree = Substitute.For<IAssetDirectoryTreeService>();
    private readonly IRollingStockService _rollingStock = Substitute.For<IRollingStockService>();
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();
    private readonly ConsistRailVehicle _leadCar = TestData.RailVehicle("1");
    private readonly ConsistRailVehicle _trailingCar = TestData.RailVehicle("2");
    private readonly RollingStockEntry _indexedVehicle = TestData.RollingStock("Class 390 DMSO");
    private readonly ConsistDetailViewModel _page;

    public ConsistDetailViewModelTests()
    {
        var vehicleIndexes = Substitute.For<IVehicleIndexService>();
        vehicleIndexes.GetCurrentIndex().Returns(_index);
        _index.SearchAsync(Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(TestData.SearchResult(1, _indexedVehicle));
        _session.GetVehicles(_consist, Arg.Any<CancellationToken>()).Returns([_leadCar, _trailingCar]);
        _directoryTree.GetDirectoryTree().Returns([]);
        _editor = Fakes.Editor(_session);

        _page = new ConsistDetailViewModel(_editor, _consist, vehicleIndexes, _launcher, _directoryTree, _rollingStock, _dialogs);
    }

    [Fact]
    public async Task Activate_LoadsVehiclesStockAndDirectories()
    {
        var provider = TestData.ProviderDirectory();
        _directoryTree.GetDirectoryTree().Returns(new ObservableCollection<BrowserDirectory> { provider });

        await _page.Activate();

        Assert.Equal([_leadCar, _trailingCar], _page.RailVehicles);
        Assert.Equal([_indexedVehicle], _page.AvailableStock);
        Assert.Equal([provider], _page.DirectoryTree);
        Assert.Equal(1, _page.MatchingVehicles);
        Assert.False(_page.HasMoreVehicles);
    }

    [Fact]
    public async Task NoVehiclesMessage_WaitsForTheFirstSearch()
    {
        _index.SearchAsync(Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(TestData.SearchResult(0));

        Assert.False(_page.HasNoVehicles);

        await _page.Activate();

        Assert.True(_page.HasNoVehicles);
    }

    [Fact]
    public async Task VehicleSearchTerm_SearchesTheIndexForTheTerm()
    {
        await _page.Activate();

        _page.VehicleSearchTerm = "dmso";

        await Wait.Until(() => _index.ReceivedCalls().Count() == 2, "the search to run");
        await _index.Received(1).SearchAsync("dmso", 200, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadMoreVehicles_SearchesTheNextPage()
    {
        _index.SearchAsync(Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(TestData.SearchResult(500, _indexedVehicle));
        await _page.Activate();

        Assert.True(_page.HasMoreVehicles);

        await _page.LoadMoreVehiclesCommand.ExecuteAsync(null);

        await _index.Received(1).SearchAsync(null, 400, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void SearchTerm_FiltersTheConsistsVehicles()
    {
        _page.RailVehicles.Reset([_leadCar, _trailingCar]);

        _page.SearchTerm = "2";

        Assert.Same(_trailingCar, Assert.Single(_page.RailVehicles));
    }

    [Fact]
    public void SelectingAVehicle_ClearsTheOtherListsSelection()
    {
        var explorerVehicle = TestData.RollingStock("Explorer Vehicle");

        _page.SelectedVehicle = _indexedVehicle;
        _page.SelectedExplorerVehicle = explorerVehicle;

        Assert.Null(_page.SelectedVehicle);

        _page.SelectedVehicle = _indexedVehicle;

        Assert.Null(_page.SelectedExplorerVehicle);
    }

    [Fact]
    public async Task AddVehicle_AddsTheSelectedStockToTheConsist()
    {
        await _page.Activate();

        Assert.False(_page.AddVehicleCommand.CanExecute(null));

        _page.SelectedVehicle = _indexedVehicle;
        await _page.AddVehicleCommand.ExecuteAsync(null);

        await _session.Received(1).AddVehicle(_consist, _indexedVehicle);
    }

    [Fact]
    public async Task AddVehicle_PrefersTheExplorerSelection()
    {
        var explorerVehicle = TestData.RollingStock("Explorer Vehicle");
        await _page.Activate();

        _page.SelectedExplorerVehicle = explorerVehicle;
        await _page.AddVehicleCommand.ExecuteAsync(null);

        await _session.Received(1).AddVehicle(_consist, explorerVehicle);
    }

    [Fact]
    public async Task ReplaceVehicle_ReplacesEverySelectedConsistVehicle()
    {
        await _page.Activate();
        _page.SelectedVehicle = _indexedVehicle;

        Assert.False(_page.ReplaceVehicleCommand.CanExecute(null));

        _page.SelectedConsistVehicles.Add(_leadCar);
        _page.SelectedConsistVehicles.Add(_trailingCar);
        await _page.ReplaceVehicleCommand.ExecuteAsync(null);

        await _session.Received(1).ReplaceVehicles(
            _consist,
            Arg.Is<IEnumerable<ConsistRailVehicle>>(targets => targets.SequenceEqual(new[] { _leadCar, _trailingCar })),
            _indexedVehicle);
    }

    [Fact]
    public async Task DeleteVehicle_NeedsExactlyOneSelectedVehicle()
    {
        await _page.Activate();

        Assert.False(_page.DeleteVehicleCommand.CanExecute(null));

        _page.SelectedConsistVehicles.Add(_trailingCar);

        Assert.True(_page.DeleteVehicleCommand.CanExecute(null));

        await _page.DeleteVehicleCommand.ExecuteAsync(null);

        await _session.Received(1).DeleteVehicle(_consist, _trailingCar);

        _page.SelectedConsistVehicles.Add(_leadCar);

        Assert.False(_page.DeleteVehicleCommand.CanExecute(null));
    }

    [Fact]
    public async Task FailedEdit_ShowsTheErrorWithoutRetry()
    {
        await _page.Activate();
        _session.AddVehicle(_consist, _indexedVehicle).Returns(Task.FromException(new IOException("Blueprint unreadable")));
        _page.SelectedVehicle = _indexedVehicle;

        await _page.AddVehicleCommand.ExecuteAsync(null);

        Assert.Equal("Blueprint unreadable", _page.Loading.ErrorMessage);
        Assert.False(_page.Loading.CanRetry);
    }

    [Fact]
    public void ExplorerCommands_FollowTheSelectedDirectory()
    {
        Assert.False(_page.OpenInExplorerCommand.CanExecute(null));
        Assert.False(_page.LoadAvailableStockCommand.CanExecute(null));

        _page.SelectedDirectory = TestData.ProviderDirectory();

        Assert.True(_page.OpenInExplorerCommand.CanExecute(null));
        Assert.False(_page.LoadAvailableStockCommand.CanExecute(null));

        _page.SelectedDirectory = TestData.ProductDirectory();

        Assert.True(_page.LoadAvailableStockCommand.CanExecute(null));
    }

    [Fact]
    public async Task LoadAvailableStock_ListsTheProductsRollingStock()
    {
        var directory = TestData.ProductDirectory();
        var explorerVehicle = TestData.RollingStock("Explorer Vehicle");
        _rollingStock.GetAvailableStock((ProductDirectory)directory.AssetDirectory, Arg.Any<CancellationToken>()).Returns([explorerVehicle]);
        _page.SelectedDirectory = directory;

        await _page.LoadAvailableStockCommand.ExecuteAsync(null);

        Assert.Equal([explorerVehicle], _page.ExplorerStock);
    }

    [Fact]
    public async Task ChangingDirectory_ClearsTheExplorerStock()
    {
        _rollingStock.GetAvailableStock(Arg.Any<ProductDirectory>(), Arg.Any<CancellationToken>()).Returns([TestData.RollingStock()]);
        _page.SelectedDirectory = TestData.ProductDirectory("WCML");
        await _page.LoadAvailableStockCommand.ExecuteAsync(null);
        _page.SelectedExplorerVehicle = _page.ExplorerStock[0];

        _page.SelectedDirectory = TestData.ProductDirectory("ECML");

        Assert.Empty(_page.ExplorerStock);
        Assert.Null(_page.SelectedExplorerVehicle);
    }

    [Fact]
    public async Task DirectoryTreeSearchTerm_MatchesDirectoriesAndTheirProducts()
    {
        var wcml = new ProductDirectory { Name = "WCML", Path = "/assets/DTG/WCML", ContainsRailVehicles = true, ContainsPreloadData = true };
        var dtg = new ProviderDirectory { Name = "DTG", Path = "/assets/DTG", Products = [wcml] };
        var other = TestData.ProviderDirectory("Armstrong Powerhouse");
        _directoryTree.GetDirectoryTree().Returns([new BrowserDirectory(dtg, _ => true), other]);
        await _page.Activate();

        _page.DirectoryTreeSearchTerm = "wcml";

        Assert.Equal("DTG", Assert.Single(_page.DirectoryTree).Name);
    }

    [Fact]
    public async Task EditCommands_FollowTheEditorOnlyWhileShown()
    {
        var notifications = 0;
        _page.ApplyChangesCommand.CanExecuteChanged += (_, _) => notifications++;
        await _page.Activate();
        await _page.Activate();

        Fakes.SetPendingChanges(_editor, true);

        Assert.Equal(1, notifications);
        Assert.True(_page.ApplyChangesCommand.CanExecute(null));

        _page.Deactivate();
        Fakes.SetPendingChanges(_editor, false);

        Assert.Equal(1, notifications);
    }

    [Fact]
    public async Task ApplyChanges_WhenTheScenarioChangedOnDisk_ShowsWhyNothingWasSaved()
    {
        Fakes.SetPendingChanges(_editor, true);
        _editor.Apply(Arg.Any<CancellationToken>()).Returns(false);

        await _page.ApplyChangesCommand.ExecuteAsync(null);

        Assert.Equal(Strings.scenario_changed_on_disk.CurrentValue, _page.Loading.ErrorMessage);
    }

    [Fact]
    public async Task DiscardChanges_DiscardsAndReloadsTheVehicles()
    {
        Fakes.SetPendingChanges(_editor, true);

        await _page.DiscardChangesCommand.ExecuteAsync(null);

        _editor.Received(1).Discard();
        Assert.Equal(2, _page.RailVehicles.Count);
    }

    [Fact]
    public async Task RefreshVehicleIndex_Declined_DoesNotIndex()
    {
        await _page.Activate();
        _dialogs.Show(Arg.Any<ConfirmationDialogViewModel>()).Returns(false);

        await _page.RefreshVehicleIndexCommand.ExecuteAsync(null);

        await _dialogs.DidNotReceive().Show(Arg.Any<VehicleIndexDialogViewModel>());
    }

    [Fact]
    public async Task RefreshVehicleIndex_Confirmed_IndexesThenSearchesAgain()
    {
        await _page.Activate();
        _dialogs.Show(Arg.Any<ConfirmationDialogViewModel>()).Returns(true);

        await _page.RefreshVehicleIndexCommand.ExecuteAsync(null);

        await _dialogs.Received(1).Show(Arg.Any<VehicleIndexDialogViewModel>());
        await _index.Received(2).SearchAsync(Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshVehicleIndex_AfterLeavingThePage_DoesNotSearch()
    {
        await _page.Activate();
        _dialogs.Show(Arg.Any<ConfirmationDialogViewModel>()).Returns(true);
        _dialogs.Show(Arg.Any<VehicleIndexDialogViewModel>()).Returns(_ =>
        {
            _page.Deactivate();

            return Task.CompletedTask;
        });

        await _page.RefreshVehicleIndexCommand.ExecuteAsync(null);

        await _index.Received(1).SearchAsync(Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}
