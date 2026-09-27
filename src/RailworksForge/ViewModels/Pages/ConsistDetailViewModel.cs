using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using RailworksForge.Core;
using RailworksForge.Core.Models;
using RailworksForge.Services;
using RailworksForge.Util;
using RailworksForge.Translations;

namespace RailworksForge.ViewModels;

public partial class ConsistDetailViewModel : ViewModelBase
{
    private readonly Scenario _scenario;
    private readonly Consist _consist;
    private readonly AssetDirectoryTreeService _directoryTree;
    private readonly ScenarioService _scenarioService;
    private readonly RollingStockService _rollingStock;
    private readonly ConsistEditService _consistEdits;
    private readonly LauncherService _launcher;

    public LoadingOperation StockLoading { get; } = new();

    public SearchableCollection<ConsistRailVehicle> RailVehicles { get; } = new(vehicle => vehicle.SearchIndex);

    public ObservableCollection<ConsistRailVehicle> SelectedConsistVehicles { get; } = [];

    public SearchableCollection<BrowserDirectory> DirectoryTree { get; } = new(MatchesDirectory);

    public ObservableCollection<RollingStockEntry> AvailableStock { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenInExplorerCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadAvailableStockCommand))]
    public partial BrowserDirectory? SelectedDirectory { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddVehicleCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReplaceVehicleCommand))]
    public partial RollingStockEntry? SelectedVehicle { get; set; }

    [ObservableProperty]
    public partial string? SearchTerm { get; set; }

    [ObservableProperty]
    public partial string? DirectoryTreeSearchTerm { get; set; }

    public ConsistDetailViewModel(
        Scenario scenario,
        Consist consist,
        AssetDirectoryTreeService directoryTree,
        ScenarioService scenarioService,
        RollingStockService rollingStock,
        ConsistEditService consistEdits,
        LauncherService launcher)
    {
        _scenario = scenario;
        _consist = consist;
        _directoryTree = directoryTree;
        _scenarioService = scenarioService;
        _rollingStock = rollingStock;
        _consistEdits = consistEdits;
        _launcher = launcher;

        SelectedConsistVehicles.CollectionChanged += (_, _) => NotifySelectionCommands();
    }

    private ConsistRailVehicle? SingleSelectedVehicle => SelectedConsistVehicles.Count is 1 ? SelectedConsistVehicles[0] : null;

    private bool HasSelectedDirectory => SelectedDirectory is not null;

    private bool IsProductDirectorySelected => SelectedDirectory?.AssetDirectory is ProductDirectory;

    private bool HasSelectedVehicle => SelectedVehicle is not null;

    private bool CanReplaceVehicle => HasSelectedVehicle && SelectedConsistVehicles.Count > 0;

    private bool HasSingleSelectedVehicle => SingleSelectedVehicle is not null;

    private bool CanOpenConsistVehicleInExplorer => SingleSelectedVehicle?.BinaryDirectoryPath is not null;

    protected override Task OnActivated()
    {
        return LoadConsist();
    }

    protected override void OnDeactivated()
    {
        StockLoading.Cancel();
    }

    partial void OnSearchTermChanged(string? value)
    {
        RailVehicles.Filter(value);
    }

    partial void OnDirectoryTreeSearchTermChanged(string? value)
    {
        DirectoryTree.Filter(value);
    }

    partial void OnSelectedDirectoryChanged(BrowserDirectory? value)
    {
        StockLoading.Cancel();
        AvailableStock.Clear();
        SelectedVehicle = null;
    }

    private static bool MatchesDirectory(BrowserDirectory directory, string searchTerm)
    {
        var directoryMatches = directory.SearchIndex.Contains(searchTerm);
        var subfolderMatches = directory.Subfolders.Any(subfolder => subfolder.SearchIndex.Contains(searchTerm));

        return directoryMatches || subfolderMatches;
    }

    private Task LoadConsist()
    {
        return Loading.RunAsync(Strings.loading_consist_vehicles.CurrentValue, async token =>
        {
            await _directoryTree.LoadDirectoryTree();
            token.ThrowIfCancellationRequested();

            var directories = _directoryTree.GetDirectoryTree().ToList();
            var vehicles = await _scenarioService.GetConsistVehicles(_scenario, _consist, token);

            return (Directories: directories, Vehicles: vehicles);
        }, result =>
        {
            DirectoryTree.Reset(result.Directories);
            RailVehicles.Reset(result.Vehicles);
        });
    }

    private async Task ReloadAfterEdit()
    {
        var shouldReload = !Loading.HasError && IsActive;

        if (shouldReload)
        {
            await LoadConsist();
        }
    }

    private void NotifySelectionCommands()
    {
        ReplaceVehicleCommand.NotifyCanExecuteChanged();
        DeleteVehicleCommand.NotifyCanExecuteChanged();
        OpenConsistVehicleInExplorerCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(IsProductDirectorySelected))]
    private Task LoadAvailableStock()
    {
        var directory = (ProductDirectory)SelectedDirectory!.AssetDirectory;

        AvailableStock.Clear();

        return StockLoading.RunAsync(
            Strings.loading_available_rolling_stock.CurrentValue,
            token => _rollingStock.GetAvailableStock(directory, token),
            AvailableStock.AddRange);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedDirectory))]
    private void OpenInExplorer()
    {
        _launcher.OpenDirectory(SelectedDirectory!.AssetDirectory.Path);
    }

    [RelayCommand(CanExecute = nameof(CanOpenConsistVehicleInExplorer))]
    private void OpenConsistVehicleInExplorer()
    {
        _launcher.OpenDirectory(SingleSelectedVehicle!.BinaryDirectoryPath!);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedVehicle))]
    private async Task AddVehicle()
    {
        var vehicle = SelectedVehicle!;

        await Loading.RunAsync(Strings.updating_consist.CurrentValue, _ => _consistEdits.AddVehicle(_scenario, vehicle));
        await ReloadAfterEdit();
    }

    [RelayCommand(CanExecute = nameof(CanReplaceVehicle))]
    private async Task ReplaceVehicle()
    {
        var replacement = SelectedVehicle!;
        var targets = SelectedConsistVehicles.ToList();

        await Loading.RunAsync(Strings.updating_consist.CurrentValue, _ => _consistEdits.ReplaceVehicles(_scenario, _consist, targets, replacement));
        await ReloadAfterEdit();
    }

    [RelayCommand(CanExecute = nameof(HasSingleSelectedVehicle))]
    private async Task DeleteVehicle()
    {
        var vehicle = SingleSelectedVehicle!;

        await Loading.RunAsync(Strings.updating_consist.CurrentValue, _ => _consistEdits.DeleteVehicle(_scenario, _consist, vehicle));
        await ReloadAfterEdit();
    }
}
