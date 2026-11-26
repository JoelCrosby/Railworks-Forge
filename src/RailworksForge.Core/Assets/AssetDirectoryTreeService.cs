using System.Collections.ObjectModel;

namespace RailworksForge.Core.Assets;

public class AssetDirectoryTreeService : IAssetDirectoryTreeService
{
    private ObservableCollection<BrowserDirectory> _directoryTree = [];
    private readonly Lock _lock = new();
    private Task? _loadTask;

    public ObservableCollection<BrowserDirectory> GetDirectoryTree()
    {
        return _directoryTree;
    }

    public void Invalidate()
    {
        lock (_lock)
        {
            _loadTask = null;
        }
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
