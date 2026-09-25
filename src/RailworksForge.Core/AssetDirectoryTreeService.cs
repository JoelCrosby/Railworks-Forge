using System.Collections.ObjectModel;

using RailworksForge.Core.Models;

namespace RailworksForge.Core;

public class AssetDirectoryTreeService
{
    private ObservableCollection<BrowserDirectory> _directoryTree = [];
    private readonly Lock _lock = new();
    private Task? _loadTask;

    public ObservableCollection<BrowserDirectory> GetDirectoryTree()
    {
        return _directoryTree;
    }

    public Task LoadDirectoryTree()
    {
        lock (_lock)
        {

            if (_loadTask is null || _loadTask.IsFaulted || _loadTask.IsCanceled)
            {
                _loadTask = Task.Run(() =>
                {
                    _directoryTree = new ObservableCollection<BrowserDirectory>(BrowserDirectory.ViewAllBrowser());
                });
            }

            return _loadTask;
        }
    }
}
