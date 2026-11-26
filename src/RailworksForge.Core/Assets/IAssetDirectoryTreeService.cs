using System.Collections.ObjectModel;

namespace RailworksForge.Core.Assets;

public interface IAssetDirectoryTreeService
{
    ObservableCollection<BrowserDirectory> GetDirectoryTree();

    void Invalidate();

    Task LoadDirectoryTree();
}
