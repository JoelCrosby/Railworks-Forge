using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using RailworksForge.Core;
using RailworksForge.Core.Models;
using RailworksForge.Services;
using RailworksForge.Util;
using RailworksForge.Translations;

namespace RailworksForge.ViewModels;

public partial class ReplaceConsistViewModel(
    AssetDirectoryTreeService directoryTree,
    PreloadConsistService preloadConsists,
    ImageService images,
    LauncherService launcher) : DialogViewModel<PreloadConsist>
{
    private readonly BackgroundImageLoader _imageLoader = new();

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

    private bool HasSelectedConsist => SelectedConsist is not null;

    protected override Task OnActivated()
    {
        return Loading.RunAsync(Strings.loading_asset_providers.CurrentValue, async _ =>
        {
            await directoryTree.LoadDirectoryTree();

            return directoryTree.GetDirectoryTree().ToList();
        }, DirectoryTree.ReplaceWith);
    }

    protected override void OnDeactivated()
    {
        StockLoading.Cancel();
        _imageLoader.Cancel();
    }

    partial void OnSelectedDirectoryChanged(BrowserDirectory? value)
    {
        StockLoading.Cancel();
        _imageLoader.Cancel();
        PreloadConsists.Clear();
        SelectedConsist = null;
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
            PreloadConsists.AddRange(rows);
            _imageLoader.Load(rows, ReadConsistImage, (row, image) => row.ImageBitmap = image);
        });
    }

    private Bitmap? ReadConsistImage(PreloadConsistViewModel row)
    {
        var leadBlueprint = row.Consist.ConsistEntries.FirstOrDefault()?.Blueprint;

        return images.GetBlueprintImage(leadBlueprint);
    }
}
