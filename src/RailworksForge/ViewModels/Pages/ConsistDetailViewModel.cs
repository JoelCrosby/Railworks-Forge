using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Echoes;

using RailworksForge.Core.Assets;
using RailworksForge.Core.Consists;
using RailworksForge.Core.Scenarios;
using RailworksForge.Core.Vehicles;
using RailworksForge.Services;
using RailworksForge.Translations;
using RailworksForge.Util;

namespace RailworksForge.ViewModels;

public partial class ConsistDetailViewModel : ViewModelBase
{
    private const int StockPageSize = 200;

    private readonly Consist _consist;
    private readonly IVehicleIndexService _vehicleIndexes;
    private readonly ILauncherService _launcher;
    private readonly IAssetDirectoryTreeService _directoryTree;
    private readonly IRollingStockService _rollingStock;
    private readonly IDialogService _dialogs;
    private readonly IImageService _images;
    private readonly BackgroundImageLoader _imageLoader = new();
    private bool _hasLoadedExplorerFolder;

    private IVehicleIndex? _vehicleIndex;

    private int _stockLimit = StockPageSize;

    public IScenarioEditor Editor { get; }

    public Consist Consist => _consist;

    public PendingChangesViewModel PendingChanges { get; }

    public SearchableCollection<ConsistVehicleViewModel> RailVehicles { get; } = new(row => row.Vehicle.SearchIndex);

    public ObservableCollection<ConsistVehicleViewModel> SelectedConsistVehicles { get; } = [];

    public int SelectedCount => SelectedConsistVehicles.Count;

    public bool HasSelection => SelectedCount > 0;

    public string SelectionText
    {
        get
        {
            var row = SingleSelectedVehicle;

            if (row is null)
            {
                return string.Format(Strings.items_selected.CurrentValue, SelectedCount);
            }

            return $"#{row.Position} {row.Vehicle.LocomotiveName}";
        }
    }

    public string VehicleCountText => string.Format(Strings.vehicle_count.CurrentValue, RailVehicles.Source.Count);

    public int MissingCount => RailVehicles.Source.Count(row => row.IsMissing);

    public bool HasMissingVehicles => MissingCount > 0;

    public string SelectMissingText => string.Format(Strings.select_missing_vehicles.CurrentValue, MissingCount);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPickerOpen))]
    [NotifyPropertyChangedFor(nameof(IsAppendMode))]
    [NotifyPropertyChangedFor(nameof(IsInsertMode))]
    [NotifyPropertyChangedFor(nameof(IsReplaceMode))]
    [NotifyPropertyChangedFor(nameof(ConfirmPickText))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmPickCommand))]
    public partial VehiclePickerMode PickerMode { get; set; }

    public bool IsPickerOpen => PickerMode is not VehiclePickerMode.Closed;

    public bool IsAppendMode => PickerMode is VehiclePickerMode.Append;

    public bool IsInsertMode => PickerMode is VehiclePickerMode.Insert;

    public bool IsReplaceMode => PickerMode is VehiclePickerMode.Replace;

    public string InsertModeText => string.Format(Strings.insert_after_position.CurrentValue, InsertAfterPosition);

    public string AddVehicleText => HasSelection ? InsertModeText : Strings.add_vehicle.CurrentValue;

    public string ReplaceModeText => string.Format(Strings.replace_selected_count.CurrentValue, SelectedCount);

    public string ConfirmPickText => PickerMode switch
    {
        VehiclePickerMode.Replace => ReplaceModeText,
        VehiclePickerMode.Insert => InsertModeText,
        _ => Strings.add_to_rear.CurrentValue,
    };

    private int InsertAfterPosition => SelectedConsistVehicles.Select(row => row.Position).DefaultIfEmpty(0).Max();

    private bool HasNoExplorerStock => ExplorerStock.Count == 0 && ExplorerLoading.IsIdle;

    public bool ShowsStockFolderPrompt => HasNoExplorerStock && !_hasLoadedExplorerFolder;

    public bool ShowsEmptyStockFolder => HasNoExplorerStock && _hasLoadedExplorerFolder;

    public override string StatusText => VehicleCountText;

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
    [NotifyCanExecuteChangedFor(nameof(ConfirmPickCommand))]
    public partial RollingStockEntry? SelectedExplorerVehicle { get; set; }

    private RollingStockEntry? ActiveSelectedVehicle => SelectedExplorerVehicle ?? SelectedVehicle;

    public RangeObservableCollection<RollingStockEntry> AvailableStock { get; } = [];

    [ObservableProperty]
    public partial string? VehicleSearchTerm { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoVehicles))]
    public partial int MatchingVehicles { get; set; }

    public bool HasMoreVehicles => AvailableStock.Count < MatchingVehicles;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoVehicles))]
    public partial bool HasSearchedStock { get; private set; }

    public bool HasNoVehicles => HasSearchedStock && MatchingVehicles == 0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmPickCommand))]
    public partial RollingStockEntry? SelectedVehicle { get; set; }

    [ObservableProperty]
    public partial string? SearchTerm { get; set; }

    public ConsistDetailViewModel(
        IScenarioEditor editor,
        Consist consist,
        IVehicleIndexService vehicleIndexes,
        ILauncherService launcher,
        IAssetDirectoryTreeService directoryTree,
        IRollingStockService rollingStock,
        IDialogService dialogs,
        IImageService images)
    {
        Editor = editor;
        _consist = consist;
        _vehicleIndexes = vehicleIndexes;
        _launcher = launcher;
        _directoryTree = directoryTree;
        _rollingStock = rollingStock;
        _dialogs = dialogs;
        _images = images;
        PendingChanges = new PendingChangesViewModel(editor);

        SelectedConsistVehicles.CollectionChanged += (_, _) => OnSelectionChanged();
        ExplorerStock.CollectionChanged += (_, _) => NotifyExplorerEmptyStates();
        ExplorerLoading.PropertyChanged += (_, _) => NotifyExplorerEmptyStates();
    }

    private ConsistVehicleViewModel? SingleSelectedVehicle => SelectedConsistVehicles.Count is 1 ? SelectedConsistVehicles[0] : null;

    private bool HasPickedStock => ActiveSelectedVehicle is not null;

    private bool CanOpenConsistVehicleInExplorer => SingleSelectedVehicle?.Vehicle.BinaryDirectoryPath is not null;

    protected override async Task OnActivated()
    {
        Editor.PropertyChanged -= OnEditorChanged;
        Editor.PropertyChanged += OnEditorChanged;
        TranslationProvider.OnCultureChanged -= OnCultureChanged;
        TranslationProvider.OnCultureChanged += OnCultureChanged;
        PendingChanges.Attach();
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

    private void NotifyExplorerEmptyStates()
    {
        OnPropertyChanged(nameof(ShowsStockFolderPrompt));
        OnPropertyChanged(nameof(ShowsEmptyStockFolder));
    }

    partial void OnSelectedDirectoryChanged(BrowserDirectory? value)
    {
        ExplorerLoading.Cancel();
        ExplorerStock.Clear();
        SelectedExplorerVehicle = null;
        _hasLoadedExplorerFolder = false;
        NotifyExplorerEmptyStates();

        if (IsProductDirectorySelected)
        {
            _ = LoadAvailableStock();
        }
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
            ShowExplorerStock);
    }

    private void ShowExplorerStock(List<RollingStockEntry> stock)
    {
        _hasLoadedExplorerFolder = true;
        ExplorerStock.ReplaceWith(stock);
        NotifyExplorerEmptyStates();
    }

    protected override void OnDeactivated()
    {
        StockLoading.Cancel();
        ExplorerLoading.Cancel();
        DirectoryLoading.Cancel();
        _imageLoader.Cancel();
        Editor.PropertyChanged -= OnEditorChanged;
        TranslationProvider.OnCultureChanged -= OnCultureChanged;
        PendingChanges.Detach();
    }

    private void OnCultureChanged(object? sender, CultureInfo culture)
    {
        OnPropertyChanged(nameof(SelectionText));
        OnPropertyChanged(nameof(VehicleCountText));
        OnPropertyChanged(nameof(SelectMissingText));
        OnPropertyChanged(nameof(StatusText));
        NotifyPickerTexts();
    }

    partial void OnSearchTermChanged(string? value)
    {
        RailVehicles.Filter(value);
        MoveTowardFrontCommand.NotifyCanExecuteChanged();
        MoveTowardRearCommand.NotifyCanExecuteChanged();
    }

    partial void OnVehicleSearchTermChanged(string? value)
    {
        _stockLimit = StockPageSize;
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

        var rows = vehicles.Select((vehicle, index) => new ConsistVehicleViewModel(vehicle, index)).ToList();

        SelectedConsistVehicles.Clear();
        RailVehicles.Reset(rows);
        OnPropertyChanged(nameof(VehicleCountText));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(MissingCount));
        OnPropertyChanged(nameof(HasMissingVehicles));
        OnPropertyChanged(nameof(SelectMissingText));
        SelectMissingCommand.NotifyCanExecuteChanged();
        _imageLoader.Load(rows, row => _images.GetBlueprintImage(row.Vehicle), (row, image) => row.ImageBitmap = image);
    }

    private void OnSelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(SelectionText));
        NotifySelectionCommands();

        // Edits clear the selection while they reload and then select the vehicles again, so only a selection the
        // user cleared moves the picker on to adding at the rear.
        if (Loading.IsIdle)
        {
            KeepPickerModeAvailable();
        }

        NotifyPickerTexts();
    }

    private void KeepPickerModeAvailable()
    {
        var isOpen = IsPickerOpen;
        var hasTarget = CanSetPickerMode(PickerMode);

        if (isOpen && !hasTarget)
        {
            PickerMode = VehiclePickerMode.Append;
        }
    }

    private void NotifyPickerTexts()
    {
        OnPropertyChanged(nameof(InsertModeText));
        OnPropertyChanged(nameof(AddVehicleText));
        OnPropertyChanged(nameof(ReplaceModeText));
        OnPropertyChanged(nameof(ConfirmPickText));
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
            HasSearchedStock = true;
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
        _stockLimit += StockPageSize;

        return SearchStock();
    }

    // Edits are buffered in the editor's session and only written to the scenario when applied.
    // True when the edit was applied and the vehicles reloaded; a failure is shown by Loading.
    private async Task<bool> Edit(Func<IScenarioEditSession, Task> edit)
    {
        var applied = false;

        await Loading.RunAsync(Strings.updating_consist.CurrentValue, async token =>
        {
            var session = await Editor.GetSession(token);
            await edit(session);

            return session.GetVehicles(_consist, token);
        }, vehicles =>
        {
            ShowVehicles(vehicles);
            applied = true;
        }, allowRetry: false);

        return applied;
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
        ConfirmPickCommand.NotifyCanExecuteChanged();
        SetPickerModeCommand.NotifyCanExecuteChanged();
        ReplaceVehiclesCommand.NotifyCanExecuteChanged();
        DeleteVehiclesCommand.NotifyCanExecuteChanged();
        OpenConsistVehicleInExplorerCommand.NotifyCanExecuteChanged();
        MoveTowardFrontCommand.NotifyCanExecuteChanged();
        MoveTowardRearCommand.NotifyCanExecuteChanged();
        FlipVehiclesCommand.NotifyCanExecuteChanged();
    }

    private List<ConsistRailVehicle> SelectedRailVehicles => SelectedConsistVehicles
        .OrderBy(row => row.Index)
        .Select(row => row.Vehicle)
        .ToList();

    // A selected vehicle can move when the place it would move into holds a vehicle that is not selected. Moving is
    // off while the list is filtered, since a vehicle would move past neighbours that aren't shown.
    private bool CanMoveSelection(int direction)
    {
        var isFiltered = !string.IsNullOrWhiteSpace(SearchTerm);

        if (isFiltered)
        {
            return false;
        }

        var rows = RailVehicles.Source;
        var selected = SelectedConsistVehicles.ToHashSet();

        return selected.Any(row =>
        {
            var target = row.Index + direction;
            var isInRange = target >= 0 && target < rows.Count;

            return isInRange && !selected.Contains(rows[target]);
        });
    }

    private bool CanMoveTowardFront => CanMoveSelection(-1);

    private bool CanMoveTowardRear => CanMoveSelection(1);

    [RelayCommand]
    private void ClearSelection()
    {
        SelectedConsistVehicles.Clear();
    }

    private bool CanSelectMissing => HasMissingVehicles;

    [RelayCommand(CanExecute = nameof(CanSelectMissing))]
    private void SelectMissing()
    {
        SearchTerm = null;
        SelectWhere(row => row.IsMissing);
    }

    private void SelectWhere(Func<ConsistVehicleViewModel, bool> predicate)
    {
        var matches = RailVehicles.AsEnumerable().Where(predicate).ToList();

        SelectedConsistVehicles.Clear();

        foreach (var row in matches)
        {
            SelectedConsistVehicles.Add(row);
        }
    }

    // Edits reload the consist as new rows, so the vehicles are selected again by their scenario ids.
    private void SelectByIds(HashSet<string> ids)
    {
        SelectWhere(row => ids.Contains(row.Vehicle.Id));
    }

    [RelayCommand(CanExecute = nameof(CanMoveTowardFront))]
    private Task MoveTowardFront()
    {
        return MoveSelection(-1);
    }

    [RelayCommand(CanExecute = nameof(CanMoveTowardRear))]
    private Task MoveTowardRear()
    {
        return MoveSelection(1);
    }

    private async Task MoveSelection(int offset)
    {
        var vehicles = SelectedRailVehicles;
        var ids = vehicles.Select(vehicle => vehicle.Id).ToHashSet();

        var applied = await Edit(session => session.MoveVehicles(_consist, vehicles, offset));

        if (applied)
        {
            SelectByIds(ids);
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task FlipVehicles()
    {
        var vehicles = SelectedRailVehicles;
        var ids = vehicles.Select(vehicle => vehicle.Id).ToHashSet();

        var applied = await Edit(session => session.FlipVehicles(_consist, vehicles));

        if (applied)
        {
            SelectByIds(ids);
        }
    }

    [RelayCommand]
    private async Task FlipRow(ConsistVehicleViewModel row)
    {
        var ids = SelectedConsistVehicles.Select(selected => selected.Vehicle.Id).ToHashSet();

        var applied = await Edit(session => session.FlipVehicles(_consist, [row.Vehicle]));

        if (applied)
        {
            SelectByIds(ids);
        }
    }

    private bool CanDeleteVehicles
    {
        get
        {
            var keepsAVehicle = SelectedCount < RailVehicles.Source.Count;

            return HasSelection && keepsAVehicle;
        }
    }

    [RelayCommand(CanExecute = nameof(CanDeleteVehicles))]
    private async Task DeleteVehicles()
    {
        var vehicles = SelectedRailVehicles;

        await Edit(session => session.DeleteVehicles(_consist, vehicles));
        KeepPickerModeAvailable();
    }

    [RelayCommand(CanExecute = nameof(CanOpenConsistVehicleInExplorer))]
    private void OpenConsistVehicleInExplorer()
    {
        _launcher.OpenDirectory(SingleSelectedVehicle!.Vehicle.BinaryDirectoryPath!);
    }

    [RelayCommand]
    private void AddVehicle()
    {
        PickerMode = HasSelection ? VehiclePickerMode.Insert : VehiclePickerMode.Append;
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void ReplaceVehicles()
    {
        PickerMode = VehiclePickerMode.Replace;
    }

    private bool CanSetPickerMode(VehiclePickerMode mode)
    {
        var modeNeedsSelection = mode is VehiclePickerMode.Insert or VehiclePickerMode.Replace;

        return !modeNeedsSelection || HasSelection;
    }

    [RelayCommand(CanExecute = nameof(CanSetPickerMode))]
    private void SetPickerMode(VehiclePickerMode mode)
    {
        PickerMode = mode;
    }

    [RelayCommand]
    private void ClosePicker()
    {
        PickerMode = VehiclePickerMode.Closed;
    }

    private bool CanConfirmPick
    {
        get
        {
            var hasTarget = CanSetPickerMode(PickerMode);

            return IsPickerOpen && HasPickedStock && hasTarget;
        }
    }

    [RelayCommand(CanExecute = nameof(CanConfirmPick))]
    private Task ConfirmPick()
    {
        var picked = ActiveSelectedVehicle!;

        return PickerMode switch
        {
            VehiclePickerMode.Replace => ReplaceSelection(picked),
            VehiclePickerMode.Insert => InsertAfterSelection(picked),
            _ => Edit(session => session.AddVehicle(_consist, picked)),
        };
    }

    private async Task ReplaceSelection(RollingStockEntry replacement)
    {
        var targets = SelectedRailVehicles;

        var applied = await Edit(session => session.ReplaceVehicles(_consist, targets, replacement));

        if (applied)
        {
            PickerMode = VehiclePickerMode.Closed;
        }
    }

    // The new vehicle is selected, so picking again inserts the next one behind it.
    private async Task InsertAfterSelection(RollingStockEntry vehicle)
    {
        var position = InsertAfterPosition;

        var applied = await Edit(session => session.AddVehicle(_consist, vehicle, position));
        var isInRange = position < RailVehicles.Source.Count;

        if (!applied || !isInRange)
        {
            return;
        }

        var inserted = RailVehicles.Source[position];

        SearchTerm = null;
        SelectWhere(row => ReferenceEquals(row, inserted));
    }
}
