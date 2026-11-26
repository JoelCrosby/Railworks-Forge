namespace RailworksForge.Core.Vehicles;

public interface IVehicleIndex
{
    Task<VehicleSearchResult> SearchAsync(string? search, int limit, CancellationToken token = default);

    Task<VehicleIndexProgress> RefreshAsync(IProgress<VehicleIndexProgress>? progress = null, CancellationToken token = default);
}
