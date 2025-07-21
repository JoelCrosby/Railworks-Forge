using System.Collections.ObjectModel;

using RailworksForge.Core.Models;

namespace RailworksForge.Core;

public class AssetDirectoryTreeService
{
    private ObservableCollection<BrowserDirectory> _directoryTree = [];

    public ObservableCollection<BrowserDirectory> GetDirectoryTree()
    {
        return _directoryTree;
    }

    public async Task LoadDirectoryTree()
    {
        _directoryTree.Clear();

        var directories = await Task.Run(() => new ObservableCollection<BrowserDirectory>(BrowserDirectory.ViewAllBrowser()));

        _directoryTree = [..directories];
    }
}
