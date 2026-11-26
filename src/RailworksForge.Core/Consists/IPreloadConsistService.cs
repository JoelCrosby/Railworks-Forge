using RailworksForge.Core.Assets;

namespace RailworksForge.Core.Consists;

public interface IPreloadConsistService
{
    Task<List<PreloadConsist>> GetPreloadConsists(BrowserDirectory directory, CancellationToken cancellationToken);
}
