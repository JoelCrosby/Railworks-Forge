using RailworksForge.Core.Assets;

namespace RailworksForge.Core.Vehicles;

public interface IRollingStockService
{
    Task<List<RollingStockEntry>> GetAvailableStock(ProductDirectory directory, CancellationToken cancellationToken);
}
