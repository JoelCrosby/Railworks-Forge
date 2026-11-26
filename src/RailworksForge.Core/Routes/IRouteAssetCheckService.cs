using RailworksForge.Core.Assets;

namespace RailworksForge.Core.Routes;

public interface IRouteAssetCheckService
{
    Task<List<Blueprint>> FindMissingAssets(Route route, IProgress<AssetCheckProgress> progress, CancellationToken cancellationToken);
}
