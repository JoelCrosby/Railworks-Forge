using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    private readonly Consist _consist;
    private readonly VehicleIndexService _vehicleIndexes;
    private readonly LauncherService _launcher;
    private readonly AssetDirectoryTreeService _directoryTree;
    private readonly RollingStockService _rollingStock;
    private readonly DialogService _dialogs;

    private VehicleIndex? _vehicleIndex;

    private int _stockLimit = 200;

    public ScenarioEditor Editor { get; }

    public LoadingOperation StockLoading { get; } = new();

    public LoadingOperation ExplorerLoading { get; } = new();

    public LoadingOperation DirectoryLoading { get; } = new();

    public SearchableCollection<BrowserDirectory> DirectoryTree { get; } = new(MatchesDirectory);

    public RangeObservableCollection<RollingStockEntry> ExplorerStock { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenInExplorerCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadAvailableStockCommand))]
    public partial BrowserDirectory? SelectedDirectory { get; set; }

    [ObservableProperty]
    public partial string? DirectoryTreeSearchTerm { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddVehicleCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReplaceVehicleCommand))]
    public partial RollingStockEntry? SelectedExplorerVehicle { get; set; }

    private RollingStockEntry? ActiveSelectedVehicle => SelectedExplorerVehicle ?? SelectedVehicle;

    public SearchableCollection<ConsistRailVehicle> RailVehicles { get; } = new(vehicle => vehicle.SearchIndex);

    public ObservableCollection<ConsistRailVehicle> SelectedConsistVehicles { get; } = [];

    public RangeObservableCollection<RollingStockEntry> AvailableStock { get; } = [];

    [ObservableProperty]
    public partial string? VehicleSearchTerm { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoVehicles))]
    public partial int MatchingVehicles { get; set; }

    public bool HasMoreVehicles => AvailableStock.Count < MatchingVehicles;

    public bool HasNoVehicles => MatchingVehicles == 0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddVehicleCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReplaceVehicleCommand))]
    public partial RollingStockEntry? SelectedVehicle { get; set; }

    [ObservableProperty]
    public partial string? SearchTerm { get; set; }

    public ConsistDetailViewModel(
        ScenarioEditor editor,
        Consist consist,
        VehicleIndexService vehicleIndexes,
        LauncherService launcher,
        AssetDirectoryTreeService directoryTree,
        RollingStockService rollingStock,
        DialogService dialogs)
    {
        Editor = editor;
        _consist = consist;
        _vehicleIndexes = vehicleIndexes;
        _launcher = launcher;
        _directoryTree = directoryTree;
        _rollingStock = rollingStock;
        _dialogs = dialogs;

        SelectedConsistVehicles.CollectionChanged += (_, _) => NotifySelectionCommands();
    }

    private ConsistRailVehicle? SingleSelectedVehicle => SelectedConsistVehicles.Count is 1 ? SelectedConsistVehicles[0] : null;

    private bool HasSelectedVehicle => ActiveSelectedVehicle is not null;

    private bool CanReplaceVehicle => HasSelectedVehicle && SelectedConsistVehicles.Count > 0;

    private bool HasSingleSelectedVehicle => SingleSelectedVehicle is not null;

    private bool CanOpenConsistVehicleInExplorer => SingleSelectedVehicle?.BinaryDirectoryPath is not null;

    protected override async Task OnActivated()
    {
        Editor.PropertyChanged -= OnEditorChanged;
        Editor.PropertyChanged += OnEditorChanged;
        _vehicleIndex = _vehicleIndexes.GetCurrentIndex();
        await Task.WhenAll(LoadConsist(), SearchStock(), LoadDirectoryTree());
    }

    private Task LoadDirectoryTree()
    {
        return DirectoryLoading.RunAsync(Strings.vehicle_loading_directories.CurrentValue, async token =>
        {
            await _directoryTree.LoadDirectoryTree().WaitAsync(token);

            return _directoryTree.GetDirectoryTree().ToList();
        }, DirectoryTree.Reset);
    }

    private static bool MatchesDirectory(BrowserDirectory directory, string searchTerm)
    {
        var directoryMatches = directory.SearchIndex.Contains(searchTerm);
        var subfolderMatches = directory.Subfolders.Any(subfolder => subfolder.SearchIndex.Contains(searchTerm));

        return directoryMatches || subfolderMatches;
    }

    partial void OnDirectoryTreeSearchTermChanged(string? value)
    {
        DirectoryTree.Filter(value);
    }

    partial void OnSelectedExplorerVehicleChanged(RollingStockEntry? value)
    {
        if (value is not null)
        {
            SelectedVehicle = null;
        }
    }

    partial void OnSelectedVehicleChanged(RollingStockEntry? value)
    {
        if (value is not null)
        {
            SelectedExplorerVehicle = null;
        }
    }

    partial void OnSelectedDirectoryChanged(BrowserDirectory? value)
    {
        ExplorerLoading.Cancel();
        ExplorerStock.Clear();
        SelectedExplorerVehicle = null;
    }

    private bool HasSelectedDirectory => SelectedDirectory is not null;

    private bool IsProductDirectorySelected => SelectedDirectory?.AssetDirectory is ProductDirectory;

    [RelayCommand(CanExecute = nameof(HasSelectedDirectory))]
    private void OpenInExplorer()
    {
        _launcher.OpenDirectory(SelectedDirectory!.AssetDirectory.Path);
    }

    [RelayCommand(CanExecute = nameof(IsProductDirectorySelected))]
    private Task LoadAvailableStock()
    {
        var directory = (ProductDirectory)SelectedDirectory!.AssetDirectory;
        ExplorerStock.Clear();
        SelectedExplorerVehicle = null;

        return ExplorerLoading.RunAsync(
            Strings.loading_available_rolling_stock.CurrentValue,
            token => _rollingStock.GetAvailableStock(directory, token),
            ExplorerStock.ReplaceWith);
    }

    protected override void OnDeactivated()
    {
        StockLoading.Cancel();
        ExplorerLoading.Cancel();
        DirectoryLoading.Cancel();
        Editor.PropertyChanged -= OnEditorChanged;
    }

    partial void OnSearchTermChanged(string? value)
    {
        RailVehicles.Filter(value);
    }

    partial void OnVehicleSearchTermChanged(string? value)
    {
        _stockLimit = 200;
        _ = SearchStock();
    }

    private Task LoadConsist()
    {
        return Loading.RunAsync(Strings.loading_consist_vehicles.CurrentValue, async token =>
        {
            var session = await Editor.GetSession(token);

            return session.GetVehicles(_consist, token);
        }, ShowVehicles);
    }

    private void ShowVehicles(List<ConsistRailVehicle> vehicles)
    {
        Editor.Update();
        RailVehicles.Reset(vehicles);
    }

    private Task SearchStock()
    {
        var index = _vehicleIndex;

        if (index is null || !IsActive)
        {
            return Task.CompletedTask;
        }

        var search = VehicleSearchTerm;
        var limit = _stockLimit;
        SelectedVehicle = null;

        return StockLoading.RunAsync(Strings.vehicle_searching.CurrentValue, async token =>
        {
            await Task.Delay(150, token);
            var result = await index.SearchAsync(search, limit, token);

            return result;
        }, result =>
        {
            AvailableStock.ReplaceWith(result.Vehicles);
            MatchingVehicles = result.Total;
            OnPropertyChanged(nameof(HasMoreVehicles));
        });
    }

    [RelayCommand]
    private async Task RefreshVehicleIndex()
    {
        var index = _vehicleIndex;

        if (index is null)
        {
            return;
        }

        var confirmation = new ConfirmationDialogViewModel
        {
            Title = Strings.vehicle_refresh.CurrentValue,
            BodyText = Strings.vehicle_index_confirmation.CurrentValue,
            AcceptLabel = Strings.vehicle_start_indexing.CurrentValue,
        };
        var confirmed = await _dialogs.Show(confirmation);

        if (!confirmed || !IsActive)
        {
            return;
        }

        var dialog = new VehicleIndexDialogViewModel(index);
        await _dialogs.Show(dialog);

        if (IsActive)
        {
            await SearchStock();
        }
    }

    [RelayCommand]
    private Task LoadMoreVehicles()
    {
        _stockLimit += 200;

        return SearchStock();
    }

    // Edits are buffered in the editor's session and only written to the scenario when applied.
    private Task Edit(Func<ScenarioEditSession, Task> edit)
    {
        return Loading.RunAsync(Strings.updating_consist.CurrentValue, async token =>
        {
            var session = await Editor.GetSession(token);
            await edit(session);

            return session.GetVehicles(_consist, token);
        }, ShowVehicles, allowRetry: false);
    }

    private bool HasPendingChanges => Editor.HasPendingChanges;

    [RelayCommand(CanExecute = nameof(HasPendingChanges))]
    private Task ApplyChanges()
    {
        return Loading.RunAsync(Strings.applying_changes.CurrentValue, Editor.Apply, applied =>
        {
            Editor.Update();

            if (!applied)
            {
                Loading.ShowError(Strings.scenario_changed_on_disk.CurrentValue);
            }
        }, allowRetry: false);
    }

    [RelayCommand(CanExecute = nameof(HasPendingChanges))]
    private Task DiscardChanges()
    {
        Editor.Discard();

        return LoadConsist();
    }

    private void OnEditorChanged(object? sender, PropertyChangedEventArgs e)
    {
        ApplyChangesCommand.NotifyCanExecuteChanged();
        DiscardChangesCommand.NotifyCanExecuteChanged();
    }

    private void NotifySelectionCommands()
    {
        ReplaceVehicleCommand.NotifyCanExecuteChanged();
        DeleteVehicleCommand.NotifyCanExecuteChanged();
        OpenConsistVehicleInExplorerCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanOpenConsistVehicleInExplorer))]
    private void OpenConsistVehicleInExplorer()
    {
        _launcher.OpenDirectory(SingleSelectedVehicle!.BinaryDirectoryPath!);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedVehicle))]
    private Task AddVehicle()
    {
        var vehicle = ActiveSelectedVehicle!;

        return Edit(session => session.AddVehicle(_consist, vehicle));
    }

    [RelayCommand(CanExecute = nameof(CanReplaceVehicle))]
    private Task ReplaceVehicle()
    {
        var replacement = ActiveSelectedVehicle!;
        var targets = SelectedConsistVehicles.ToList();

        return Edit(session => session.ReplaceVehicles(_consist, targets, replacement));
    }

    [RelayCommand(CanExecute = nameof(HasSingleSelectedVehicle))]
    private Task DeleteVehicle()
    {
        var vehicle = SingleSelectedVehicle!;

        return Edit(session => session.DeleteVehicle(_consist, vehicle));
    }
}
