using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using RailworksForge.Core.Assets;
using RailworksForge.Core.Consists;
using RailworksForge.Services;
using RailworksForge.Translations;
using RailworksForge.Util;

namespace RailworksForge.ViewModels;

public partial class ReplaceConsistViewModel(
    IAssetDirectoryTreeService directoryTree,
    IPreloadConsistService preloadConsists,
    IImageService images,
    ILauncherService launcher) : DialogViewModel<PreloadConsist>
{
    private readonly BackgroundImageLoader _imageLoader = new();
    private bool _hasLoadedFolder;

    public LoadingOperation StockLoading { get; } = new();

    public RangeObservableCollection<BrowserDirectory> DirectoryTree { get; } = [];

    public RangeObservableCollection<PreloadConsistViewModel> PreloadConsists { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenInExplorerCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadAvailableStockCommand))]
    public partial BrowserDirectory? SelectedDirectory { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ReplaceConsistCommand))]
    public partial PreloadConsistViewModel? SelectedConsist { get; set; }

    private bool HasSelectedDirectory => SelectedDirectory is not null;

    private bool HasNoConsists => PreloadConsists.Count == 0 && StockLoading.IsIdle;

    public bool ShowsFolderPrompt => HasNoConsists && !_hasLoadedFolder;

    public bool ShowsEmptyFolder => HasNoConsists && _hasLoadedFolder;

    private bool HasSelectedConsist => SelectedConsist is not null;

    protected override Task OnActivated()
    {
        PreloadConsists.CollectionChanged -= OnConsistsChanged;
        PreloadConsists.CollectionChanged += OnConsistsChanged;
        StockLoading.PropertyChanged -= OnConsistsChanged;
        StockLoading.PropertyChanged += OnConsistsChanged;

        return Loading.RunAsync(Strings.loading_asset_providers.CurrentValue, async token =>
        {
            await directoryTree.LoadDirectoryTree().WaitAsync(token);

            return directoryTree.GetDirectoryTree().ToList();
        }, DirectoryTree.ReplaceWith);
    }

    protected override void OnDeactivated()
    {
        StockLoading.Cancel();
        _imageLoader.Cancel();
        PreloadConsists.CollectionChanged -= OnConsistsChanged;
        StockLoading.PropertyChanged -= OnConsistsChanged;
    }

    private void OnConsistsChanged(object? sender, EventArgs e)
    {
        NotifyEmptyStates();
    }

    private void NotifyEmptyStates()
    {
        OnPropertyChanged(nameof(ShowsFolderPrompt));
        OnPropertyChanged(nameof(ShowsEmptyFolder));
    }

    partial void OnSelectedDirectoryChanged(BrowserDirectory? value)
    {
        StockLoading.Cancel();
        _imageLoader.Cancel();
        PreloadConsists.Clear();
        SelectedConsist = null;
        _hasLoadedFolder = false;
        NotifyEmptyStates();

        if (value?.AssetDirectory is ProductDirectory)
        {
            _ = LoadAvailableStock();
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedConsist))]
    private void ReplaceConsist()
    {
        Close(SelectedConsist!.Consist);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedDirectory))]
    private void OpenInExplorer()
    {
        launcher.OpenDirectory(SelectedDirectory!.AssetDirectory.Path);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedDirectory))]
    private Task LoadAvailableStock()
    {
        var directory = SelectedDirectory!;

        PreloadConsists.Clear();

        return StockLoading.RunAsync(Strings.loading_replacement_consists.CurrentValue, async token =>
        {
            var consists = await preloadConsists.GetPreloadConsists(directory, token);

            return consists.ConvertAll(consist => new PreloadConsistViewModel(consist));
        }, rows =>
        {
            _hasLoadedFolder = true;
            PreloadConsists.ReplaceWith(rows);
            NotifyEmptyStates();
            _imageLoader.Load(rows, ReadConsistImage, (row, image) => row.ImageBitmap = image);
        });
    }

    private Bitmap? ReadConsistImage(PreloadConsistViewModel row)
    {
        var leadBlueprint = row.Consist.ConsistEntries.FirstOrDefault()?.Blueprint;

        return images.GetBlueprintImage(leadBlueprint);
    }
}
