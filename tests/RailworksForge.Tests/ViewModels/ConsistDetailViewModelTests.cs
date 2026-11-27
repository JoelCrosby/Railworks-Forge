using System.Collections.ObjectModel;

using NSubstitute;

using RailworksForge.Core.Assets;
using RailworksForge.Core.Common;
using RailworksForge.Core.Extensions;
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

        _page = new ConsistDetailViewModel(
            _editor,
            _consist,
            vehicleIndexes,
            _launcher,
            _directoryTree,
            _rollingStock,
            _dialogs,
            Substitute.For<IImageService>());
    }

    [Fact]
    public async Task Activate_LoadsVehiclesStockAndDirectories()
    {
        var provider = TestData.ProviderDirectory();
        _directoryTree.GetDirectoryTree().Returns(new ObservableCollection<BrowserDirectory> { provider });

        await _page.Activate();

        Assert.Equal([_leadCar, _trailingCar], _page.RailVehicles.Select(row => row.Vehicle));
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
    public async Task SearchTerm_FiltersTheConsistsVehicles()
    {
        await _page.Activate();

        _page.SearchTerm = "2";

        Assert.Same(_trailingCar, Assert.Single(_page.RailVehicles).Vehicle);
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
    public async Task AddVehicle_WithNothingSelected_OpensThePickerToAddAtTheRear()
    {
        await _page.Activate();

        _page.AddVehicleCommand.Execute(null);

        Assert.Equal(VehiclePickerMode.Append, _page.PickerMode);
        Assert.False(_page.ConfirmPickCommand.CanExecute(null));

        _page.SelectedVehicle = _indexedVehicle;
        await _page.ConfirmPickCommand.ExecuteAsync(null);

        await _session.Received(1).AddVehicle(_consist, _indexedVehicle, null);
        Assert.True(_page.IsPickerOpen);
    }

    [Fact]
    public async Task ConfirmPick_PrefersTheExplorerSelection()
    {
        var explorerVehicle = TestData.RollingStock("Explorer Vehicle");
        await _page.Activate();
        _page.AddVehicleCommand.Execute(null);

        _page.SelectedExplorerVehicle = explorerVehicle;
        await _page.ConfirmPickCommand.ExecuteAsync(null);

        await _session.Received(1).AddVehicle(_consist, explorerVehicle, null);
    }

    [Fact]
    public async Task AddVehicle_WithASelection_InsertsBehindItAndSelectsTheNewVehicle()
    {
        var inserted = TestData.RailVehicle("3");
        await _page.Activate();
        SelectRows(0);

        _page.AddVehicleCommand.Execute(null);

        Assert.Equal(VehiclePickerMode.Insert, _page.PickerMode);

        _session.GetVehicles(_consist, Arg.Any<CancellationToken>()).Returns([_leadCar, inserted, _trailingCar]);
        _page.SelectedVehicle = _indexedVehicle;
        await _page.ConfirmPickCommand.ExecuteAsync(null);

        await _session.Received(1).AddVehicle(_consist, _indexedVehicle, 1);
        Assert.Same(inserted, Assert.Single(_page.SelectedConsistVehicles).Vehicle);
    }

    [Fact]
    public async Task ReplaceVehicles_ReplacesEverySelectedVehicleAndClosesThePicker()
    {
        await _page.Activate();

        Assert.False(_page.ReplaceVehiclesCommand.CanExecute(null));

        SelectRows(1, 0);
        _page.ReplaceVehiclesCommand.Execute(null);
        _page.SelectedVehicle = _indexedVehicle;
        await _page.ConfirmPickCommand.ExecuteAsync(null);

        await _session.Received(1).ReplaceVehicles(
            _consist,
            Arg.Is<IEnumerable<ConsistRailVehicle>>(targets => targets.SequenceEqual(new[] { _leadCar, _trailingCar })),
            _indexedVehicle);
        Assert.False(_page.IsPickerOpen);
    }

    [Fact]
    public async Task PickerModesThatNeedASelection_AreUnavailableWithoutOne()
    {
        await _page.Activate();

        Assert.True(_page.SetPickerModeCommand.CanExecute(VehiclePickerMode.Append));
        Assert.False(_page.SetPickerModeCommand.CanExecute(VehiclePickerMode.Insert));
        Assert.False(_page.SetPickerModeCommand.CanExecute(VehiclePickerMode.Replace));

        SelectRows(0);

        Assert.True(_page.SetPickerModeCommand.CanExecute(VehiclePickerMode.Replace));
    }

    [Fact]
    public async Task DeleteVehicles_RemovesEverySelectedVehicle()
    {
        var middleCar = TestData.RailVehicle("3");
        _session.GetVehicles(_consist, Arg.Any<CancellationToken>()).Returns([_leadCar, middleCar, _trailingCar]);
        await _page.Activate();

        Assert.False(_page.DeleteVehiclesCommand.CanExecute(null));

        SelectRows(2, 0);
        await _page.DeleteVehiclesCommand.ExecuteAsync(null);

        await _session.Received(1).DeleteVehicles(
            _consist,
            Arg.Is<IEnumerable<ConsistRailVehicle>>(vehicles => vehicles.SequenceEqual(new[] { _leadCar, _trailingCar })));
    }

    [Fact]
    public async Task DeleteVehicles_CannotRemoveTheWholeTrain()
    {
        await _page.Activate();

        SelectRows(0, 1);

        Assert.False(_page.DeleteVehiclesCommand.CanExecute(null));
    }

    [Fact]
    public async Task FailedInsert_KeepsTheSelection()
    {
        await _page.Activate();
        _session.AddVehicle(_consist, _indexedVehicle, 1).Returns(Task.FromException(new IOException("Blueprint unreadable")));
        SelectRows(0);
        _page.AddVehicleCommand.Execute(null);
        _page.SelectedVehicle = _indexedVehicle;

        await _page.ConfirmPickCommand.ExecuteAsync(null);

        Assert.Same(_leadCar, Assert.Single(_page.SelectedConsistVehicles).Vehicle);
        Assert.Equal(VehiclePickerMode.Insert, _page.PickerMode);
    }

    [Fact]
    public async Task FailedReplace_LeavesThePickerOpen()
    {
        await _page.Activate();
        _session.ReplaceVehicles(_consist, Arg.Any<IEnumerable<ConsistRailVehicle>>(), _indexedVehicle)
            .Returns(Task.FromException(new IOException("Blueprint unreadable")));
        SelectRows(0);
        _page.ReplaceVehiclesCommand.Execute(null);
        _page.SelectedVehicle = _indexedVehicle;

        await _page.ConfirmPickCommand.ExecuteAsync(null);

        Assert.Equal(VehiclePickerMode.Replace, _page.PickerMode);
    }

    [Fact]
    public async Task ClearingTheSelection_MovesThePickerToAddingAtTheRear()
    {
        await _page.Activate();
        SelectRows(0);
        _page.ReplaceVehiclesCommand.Execute(null);

        _page.ClearSelectionCommand.Execute(null);

        Assert.Equal(VehiclePickerMode.Append, _page.PickerMode);
    }

    [Fact]
    public async Task AddVehicleText_NamesWhereTheVehicleGoes()
    {
        await _page.Activate();

        Assert.Equal(Strings.add_vehicle.CurrentValue, _page.AddVehicleText);

        SelectRows(0);

        Assert.Equal(string.Format(Strings.insert_after_position.CurrentValue, 1), _page.AddVehicleText);
    }

    [Fact]
    public async Task SelectionText_NamesASingleVehicle()
    {
        await _page.Activate();

        SelectRows(1);

        Assert.Equal("#2 Class 390", _page.SelectionText);

        SelectRows(0, 1);

        Assert.Equal(string.Format(Strings.items_selected.CurrentValue, 2), _page.SelectionText);
    }

    [Fact]
    public async Task MoveCommands_AreOffWhileTheListIsFiltered()
    {
        await _page.Activate();
        SelectRows(1);

        _page.SearchTerm = "2";

        Assert.False(_page.MoveTowardFrontCommand.CanExecute(null));

        _page.SearchTerm = null;

        Assert.True(_page.MoveTowardFrontCommand.CanExecute(null));
    }

    [Fact]
    public async Task MoveCommands_DependOnWhereTheSelectionCanMove()
    {
        await _page.Activate();

        SelectRows(0);

        Assert.False(_page.MoveTowardFrontCommand.CanExecute(null));
        Assert.True(_page.MoveTowardRearCommand.CanExecute(null));

        SelectRows(1);

        Assert.True(_page.MoveTowardFrontCommand.CanExecute(null));
        Assert.False(_page.MoveTowardRearCommand.CanExecute(null));

        SelectRows(0, 1);

        Assert.False(_page.MoveTowardFrontCommand.CanExecute(null));
        Assert.False(_page.MoveTowardRearCommand.CanExecute(null));
    }

    [Fact]
    public async Task MoveTowardRear_MovesTheSelectionAndKeepsItSelected()
    {
        await _page.Activate();
        SelectRows(0);

        await _page.MoveTowardRearCommand.ExecuteAsync(null);

        await _session.Received(1).MoveVehicles(
            _consist,
            Arg.Is<IEnumerable<ConsistRailVehicle>>(vehicles => vehicles.SequenceEqual(new[] { _leadCar })),
            1);
        Assert.Same(_leadCar, Assert.Single(_page.SelectedConsistVehicles).Vehicle);
    }

    [Fact]
    public async Task MoveTowardFront_MovesTheSelectionOnePlaceForward()
    {
        await _page.Activate();
        SelectRows(1);

        await _page.MoveTowardFrontCommand.ExecuteAsync(null);

        await _session.Received(1).MoveVehicles(
            _consist,
            Arg.Is<IEnumerable<ConsistRailVehicle>>(vehicles => vehicles.SequenceEqual(new[] { _trailingCar })),
            -1);
    }

    [Fact]
    public async Task FlipVehicles_FlipsEverySelectedVehicle()
    {
        await _page.Activate();
        SelectRows(0, 1);

        await _page.FlipVehiclesCommand.ExecuteAsync(null);

        await _session.Received(1).FlipVehicles(
            _consist,
            Arg.Is<IEnumerable<ConsistRailVehicle>>(vehicles => vehicles.SequenceEqual(new[] { _leadCar, _trailingCar })));
        Assert.Equal(2, _page.SelectedCount);
    }

    [Fact]
    public async Task FlipRow_FlipsThatVehicleAndKeepsTheSelection()
    {
        await _page.Activate();
        SelectRows(0);

        await _page.FlipRowCommand.ExecuteAsync(_page.RailVehicles[1]);

        await _session.Received(1).FlipVehicles(
            _consist,
            Arg.Is<IEnumerable<ConsistRailVehicle>>(vehicles => vehicles.SequenceEqual(new[] { _trailingCar })));
        Assert.Same(_leadCar, Assert.Single(_page.SelectedConsistVehicles).Vehicle);
    }

    [Fact]
    public async Task SelectMissing_SelectsOnlyTheMissingVehicles()
    {
        var missing = new ConsistRailVehicle
        {
            Id = "3",
            LocomotiveName = "Missing wagon",
            EntityID = "3",
            SearchIndex = "3",
            BlueprintSetIdProvider = string.Empty,
            BlueprintSetIdProduct = string.Empty,
            BlueprintId = @"RailVehicles\Wagon.xml",
        };
        var leadCarKey = Path.Join(_leadCar.BlueprintSetIdProvider, _leadCar.BlueprintSetIdProduct, _leadCar.RelativeBinaryPath).NormalisePath();
        Cache.BlueprintAcquisitionStates[leadCarKey] = AcquisitionState.Found;
        _session.GetVehicles(_consist, Arg.Any<CancellationToken>()).Returns([_leadCar, missing]);

        try
        {
            await _page.Activate();

            Assert.Equal(1, _page.MissingCount);

            _page.SearchTerm = "1";
            _page.SelectMissingCommand.Execute(null);

            Assert.Same(missing, Assert.Single(_page.SelectedConsistVehicles).Vehicle);
        }
        finally
        {
            Cache.ClearAcquisitionStates();
        }
    }

    [Fact]
    public async Task ClearSelection_EmptiesTheSelection()
    {
        await _page.Activate();
        SelectRows(0);

        _page.ClearSelectionCommand.Execute(null);

        Assert.Empty(_page.SelectedConsistVehicles);
    }

    [Fact]
    public async Task SelectingAProductDirectory_LoadsItsStock()
    {
        var vehicle = TestData.RollingStock();
        _rollingStock.GetAvailableStock(Arg.Any<ProductDirectory>(), Arg.Any<CancellationToken>()).Returns([vehicle]);

        _page.SelectedDirectory = TestData.ProductDirectory("WCML");
        await Wait.Until(() => _page.ExplorerStock.Count > 0);

        Assert.Same(vehicle, Assert.Single(_page.ExplorerStock));
        Assert.False(_page.ShowsStockFolderPrompt);
        Assert.False(_page.ShowsEmptyStockFolder);
    }

    [Fact]
    public void StockFolderPrompt_ShowsUntilAFolderIsLoaded()
    {
        Assert.True(_page.ShowsStockFolderPrompt);

        _page.SelectedDirectory = TestData.ProviderDirectory();

        Assert.True(_page.ShowsStockFolderPrompt);
        Assert.False(_page.ShowsEmptyStockFolder);
    }

    [Fact]
    public async Task EmptyProductDirectory_SaysTheFolderHasNoVehicles()
    {
        _rollingStock.GetAvailableStock(Arg.Any<ProductDirectory>(), Arg.Any<CancellationToken>()).Returns([]);

        _page.SelectedDirectory = TestData.ProductDirectory("WCML");
        await Wait.Until(() => _page.ShowsEmptyStockFolder);

        Assert.False(_page.ShowsStockFolderPrompt);

        _page.SelectedDirectory = null;

        Assert.True(_page.ShowsStockFolderPrompt);
        Assert.False(_page.ShowsEmptyStockFolder);
    }

    [Fact]
    public async Task FailedEdit_ShowsTheErrorWithoutRetry()
    {
        await _page.Activate();
        _session.AddVehicle(_consist, _indexedVehicle, null).Returns(Task.FromException(new IOException("Blueprint unreadable")));
        _page.AddVehicleCommand.Execute(null);
        _page.SelectedVehicle = _indexedVehicle;

        await _page.ConfirmPickCommand.ExecuteAsync(null);

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

        _page.SelectedDirectory = TestData.ProviderDirectory("LNER");

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

    private void SelectRows(params int[] indexes)
    {
        _page.SelectedConsistVehicles.Clear();

        foreach (var index in indexes)
        {
            _page.SelectedConsistVehicles.Add(_page.RailVehicles[index]);
        }
    }
}
