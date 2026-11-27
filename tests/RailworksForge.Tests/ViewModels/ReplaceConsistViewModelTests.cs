using System.Collections.ObjectModel;

using NSubstitute;

using RailworksForge.Core.Assets;
using RailworksForge.Core.Consists;
using RailworksForge.Services;
using RailworksForge.Tests.Support;
using RailworksForge.ViewModels;

namespace RailworksForge.Tests.ViewModels;

public class ReplaceConsistViewModelTests
{
    private readonly IAssetDirectoryTreeService _directoryTree = Substitute.For<IAssetDirectoryTreeService>();
    private readonly IPreloadConsistService _preloadConsists = Substitute.For<IPreloadConsistService>();
    private readonly ILauncherService _launcher = Substitute.For<ILauncherService>();
    private readonly ReplaceConsistViewModel _dialog;

    public ReplaceConsistViewModelTests()
    {
        _dialog = new ReplaceConsistViewModel(_directoryTree, _preloadConsists, Substitute.For<IImageService>(), _launcher);
    }

    [Fact]
    public async Task Activate_LoadsTheAssetDirectoryTree()
    {
        var provider = TestData.ProviderDirectory();
        _directoryTree.GetDirectoryTree().Returns(new ObservableCollection<BrowserDirectory> { provider });

        await _dialog.Activate();

        await _directoryTree.Received(1).LoadDirectoryTree();
        Assert.Equal([provider], _dialog.DirectoryTree);
    }

    [Fact]
    public void DirectoryCommands_NeedASelectedDirectory()
    {
        Assert.False(_dialog.LoadAvailableStockCommand.CanExecute(null));
        Assert.False(_dialog.OpenInExplorerCommand.CanExecute(null));

        _dialog.SelectedDirectory = TestData.ProductDirectory();

        Assert.True(_dialog.LoadAvailableStockCommand.CanExecute(null));
        Assert.True(_dialog.OpenInExplorerCommand.CanExecute(null));
    }

    [Fact]
    public void OpenInExplorer_OpensTheSelectedDirectory()
    {
        _dialog.SelectedDirectory = TestData.ProductDirectory("WCML");

        _dialog.OpenInExplorerCommand.Execute(null);

        _launcher.Received(1).OpenDirectory("/assets/DTG/WCML");
    }

    [Fact]
    public async Task LoadAvailableStock_ListsTheDirectorysPreloadConsists()
    {
        var directory = TestData.ProductDirectory();
        var consist = TestData.PreloadConsist();
        _preloadConsists.GetPreloadConsists(directory, Arg.Any<CancellationToken>()).Returns([consist]);
        _dialog.SelectedDirectory = directory;

        await _dialog.LoadAvailableStockCommand.ExecuteAsync(null);

        var row = Assert.Single(_dialog.PreloadConsists);
        Assert.Same(consist, row.Consist);
    }

    [Fact]
    public async Task ChangingDirectory_ClearsTheListedConsistsAndSelection()
    {
        _preloadConsists.GetPreloadConsists(Arg.Any<BrowserDirectory>(), Arg.Any<CancellationToken>()).Returns([TestData.PreloadConsist()]);
        _dialog.SelectedDirectory = TestData.ProductDirectory("WCML");
        await _dialog.LoadAvailableStockCommand.ExecuteAsync(null);
        _dialog.SelectedConsist = _dialog.PreloadConsists[0];

        _dialog.SelectedDirectory = TestData.ProviderDirectory("LNER");

        Assert.Empty(_dialog.PreloadConsists);
        Assert.Null(_dialog.SelectedConsist);
        Assert.False(_dialog.ReplaceConsistCommand.CanExecute(null));
    }

    [Fact]
    public void ReplaceConsist_ClosesWithTheSelectedConsist()
    {
        var consist = TestData.PreloadConsist();
        object? result = null;
        _dialog.CloseRequested += value => result = value;

        Assert.False(_dialog.ReplaceConsistCommand.CanExecute(null));

        _dialog.SelectedConsist = new PreloadConsistViewModel(consist);
        _dialog.ReplaceConsistCommand.Execute(null);

        Assert.Same(consist, result);
    }

    [Fact]
    public async Task Deactivate_StopsLoadingStock()
    {
        var entered = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        _preloadConsists
            .GetPreloadConsists(Arg.Any<BrowserDirectory>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var token = call.Arg<CancellationToken>();

                entered.SetResult(token);

                return Task.Delay(Timeout.Infinite, token).ContinueWith(_ => new List<PreloadConsist>(), TaskScheduler.Default);
            });
        _dialog.SelectedDirectory = TestData.ProductDirectory();
        var load = _dialog.LoadAvailableStockCommand.ExecuteAsync(null);
        var loadToken = await entered.Task;

        _dialog.Deactivate();
        await load;

        Assert.True(loadToken.IsCancellationRequested);
        Assert.False(_dialog.StockLoading.IsLoading);
        Assert.Empty(_dialog.PreloadConsists);
    }

    [Fact]
    public async Task SelectingAProductDirectory_LoadsItsConsists()
    {
        var consist = TestData.PreloadConsist();
        _preloadConsists.GetPreloadConsists(Arg.Any<BrowserDirectory>(), Arg.Any<CancellationToken>()).Returns([consist]);

        _dialog.SelectedDirectory = TestData.ProductDirectory("WCML");
        await Wait.Until(() => _dialog.PreloadConsists.Count > 0);

        Assert.Same(consist, Assert.Single(_dialog.PreloadConsists).Consist);
        Assert.False(_dialog.ShowsFolderPrompt);
        Assert.False(_dialog.ShowsEmptyFolder);
    }

    [Fact]
    public async Task EmptyDirectory_SaysTheFolderHasNoConsists()
    {
        _preloadConsists.GetPreloadConsists(Arg.Any<BrowserDirectory>(), Arg.Any<CancellationToken>()).Returns([]);

        Assert.True(_dialog.ShowsFolderPrompt);

        _dialog.SelectedDirectory = TestData.ProductDirectory("WCML");
        await Wait.Until(() => _dialog.ShowsEmptyFolder);

        Assert.False(_dialog.ShowsFolderPrompt);
    }
}
