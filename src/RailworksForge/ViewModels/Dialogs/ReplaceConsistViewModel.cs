using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using RailworksForge.Core;
using RailworksForge.Core.Models;
using RailworksForge.Services;
using RailworksForge.Util;

using Serilog;

namespace RailworksForge.ViewModels;

public partial class ReplaceConsistViewModel(
    AssetDirectoryTreeService directoryTree,
    PreloadConsistService preloadConsists,
    LauncherService launcher) : DialogViewModel<PreloadConsist>
{
    public LoadingOperation StockLoading { get; } = new();

    public ObservableCollection<BrowserDirectory> DirectoryTree { get; } = [];

    public ObservableCollection<PreloadConsistViewModel> PreloadConsists { get; } = [];

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
        return Loading.RunAsync("Loading asset providers…", async _ =>
        {
            await directoryTree.LoadDirectoryTree();

            return directoryTree.GetDirectoryTree().ToList();
        }, DirectoryTree.ReplaceWith);
    }

    protected override void OnDeactivated()
    {
        StockLoading.Cancel();
    }

    partial void OnSelectedDirectoryChanged(BrowserDirectory? value)
    {
        StockLoading.Cancel();
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

        return StockLoading.RunAsync("Loading replacement consists…", async token =>
        {
            var consists = await preloadConsists.GetPreloadConsists(directory, token);
            var models = consists.ConvertAll(consist => new PreloadConsistViewModel(consist));

            LoadImages(models);

            return models;
        }, PreloadConsists.AddRange);
    }

    private static void LoadImages(IEnumerable<PreloadConsistViewModel> items)
    {
        try
        {
            foreach (var item in items)
            {
                item.LoadImage();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "an error occured while trying to load stock images");
        }
    }
}
