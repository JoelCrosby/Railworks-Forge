using RailworksForge.Core.Common;

namespace RailworksForge.Core.Vehicles;

public sealed class VehicleIndexService : IVehicleIndexService
{
    private readonly Dictionary<string, VehicleIndex> _indexes = new(StringComparer.Ordinal);

    public IVehicleIndex GetCurrentIndex()
    {
        var assetsPath = Paths.GetAssetsDirectory();

        lock (_indexes)
        {

            if (!_indexes.TryGetValue(assetsPath, out var index))
            {
                index = new VehicleIndex(assetsPath, Paths.GetCacheFolder());
                _indexes.Add(assetsPath, index);
            }

            return index;
        }
    }
}
