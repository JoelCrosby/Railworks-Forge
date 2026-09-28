namespace RailworksForge.Core;

public sealed class VehicleIndexService
{
    private readonly Dictionary<string, VehicleIndex> _indexes = new(StringComparer.Ordinal);

    public VehicleIndex GetCurrentIndex()
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
