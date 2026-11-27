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
    private bool _isSyncingSelection;
    private bool _hasLoadedExplorerFolder;

    private IVehicleIndex? _vehicleIndex;

    private int _stockLimit = StockPageSize;

    public IScenarioEditor Editor { get; }

    public Consist Consist => _consist;

    public PendingChangesViewModel PendingChanges { get; }

    public RangeObservableCollection<ConsistVehicleViewModel> StripVehicles { get; } = [];

    [ObservableProperty]
    public partial ConsistVehicleViewModel? SelectedStripVehicle { get; set; }

    public int SelectedCount => SelectedConsistVehicles.Count;

    public string SelectionText
    {
        get
        {
            var vehicle = SingleSelectedVehicle;

            if (vehicle is null)
            {
                return string.Format(Strings.items_selected.CurrentValue, SelectedCount);
            }

            return $"#{vehicle.Index} {vehicle.LocomotiveName}";
        }
    }

    public string VehicleCountText => string.Format(Strings.vehicle_count.CurrentValue, RailVehicles.Source.Count);

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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoVehicles))]
    public partial bool HasSearchedStock { get; private set; }

    public bool HasNoVehicles => HasSearchedStock && MatchingVehicles == 0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddVehicleCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReplaceVehicleCommand))]
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

    private ConsistRailVehicle? SingleSelectedVehicle => SelectedConsistVehicles.Count is 1 ? SelectedConsistVehicles[0] : null;

    private bool HasSelectedVehicle => ActiveSelectedVehicle is not null;

    private bool CanReplaceVehicle => HasSelectedVehicle && SelectedConsistVehicles.Count > 0;

    private bool HasSingleSelectedVehicle => SingleSelectedVehicle is not null;

    private bool CanOpenConsistVehicleInExplorer => SingleSelectedVehicle?.BinaryDirectoryPath is not null;

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
        foreach (var vehicle in StripVehicles)
        {
            vehicle.RefreshCaption();
        }

        OnPropertyChanged(nameof(SelectionText));
        OnPropertyChanged(nameof(VehicleCountText));
        OnPropertyChanged(nameof(StatusText));
    }

    partial void OnSearchTermChanged(string? value)
    {
        RailVehicles.Filter(value);
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
        RailVehicles.Reset(vehicles);

        var stripVehicles = vehicles.Select((vehicle, index) => new ConsistVehicleViewModel(vehicle, index)).ToList();

        StripVehicles.ReplaceWith(stripVehicles);
        OnPropertyChanged(nameof(VehicleCountText));
        OnPropertyChanged(nameof(StatusText));
        _imageLoader.Load(stripVehicles, item => _images.GetBlueprintImage(item.Vehicle), (item, image) => item.ImageBitmap = image);
    }

    private void OnSelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(SelectionText));
        NotifySelectionCommands();
        SyncStripSelection();
    }

    private void SyncStripSelection()
    {
        if (_isSyncingSelection)
        {
            return;
        }

        var single = SingleSelectedVehicle;
        var stripVehicle = StripVehicles.FirstOrDefault(item => ReferenceEquals(item.Vehicle, single));

        _isSyncingSelection = true;
        SelectedStripVehicle = stripVehicle;
        _isSyncingSelection = false;
    }

    partial void OnSelectedStripVehicleChanged(ConsistVehicleViewModel? value)
    {
        if (_isSyncingSelection || value is null)
        {
            return;
        }

        _isSyncingSelection = true;
        SelectedConsistVehicles.Clear();
        SelectedConsistVehicles.Add(value.Vehicle);
        _isSyncingSelection = false;
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
    private Task Edit(Func<IScenarioEditSession, Task> edit)
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
        MoveVehicleForwardCommand.NotifyCanExecuteChanged();
        MoveVehicleBackCommand.NotifyCanExecuteChanged();
        FlipVehicleCommand.NotifyCanExecuteChanged();
    }

    private int SingleSelectedPosition
    {
        get
        {
            var vehicle = SingleSelectedVehicle;

            if (vehicle is null)
            {
                return -1;
            }

            var vehicles = RailVehicles.Source.ToList();

            return vehicles.IndexOf(vehicle);
        }
    }

    private bool CanMoveVehicleForward => SingleSelectedPosition > 0;

    private bool CanMoveVehicleBack
    {
        get
        {
            var position = SingleSelectedPosition;
            var isLastVehicle = position == RailVehicles.Source.Count - 1;

            return position >= 0 && !isLastVehicle;
        }
    }

    [RelayCommand]
    private void ClearSelection()
    {
        SelectedConsistVehicles.Clear();
    }

    [RelayCommand(CanExecute = nameof(CanMoveVehicleForward))]
    private Task MoveVehicleForward()
    {
        return MoveSelectedVehicle(-1);
    }

    [RelayCommand(CanExecute = nameof(CanMoveVehicleBack))]
    private Task MoveVehicleBack()
    {
        return MoveSelectedVehicle(1);
    }

    private async Task MoveSelectedVehicle(int offset)
    {
        var vehicle = SingleSelectedVehicle!;
        var newPosition = SingleSelectedPosition + offset;

        await Edit(session => session.MoveVehicle(_consist, vehicle, newPosition));
        SelectVehicleAt(newPosition);
    }

    [RelayCommand(CanExecute = nameof(HasSingleSelectedVehicle))]
    private async Task FlipVehicle()
    {
        var vehicle = SingleSelectedVehicle!;
        var position = SingleSelectedPosition;

        await Edit(session => session.FlipVehicle(_consist, vehicle));
        SelectVehicleAt(position);
    }

    // Edits reload the consist as new vehicle objects, so the moved or flipped vehicle is selected again by position.
    private void SelectVehicleAt(int position)
    {
        var isInRange = position >= 0 && position < StripVehicles.Count;

        if (!isInRange)
        {
            return;
        }

        SelectedStripVehicle = StripVehicles[position];
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
