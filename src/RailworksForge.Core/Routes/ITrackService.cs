namespace RailworksForge.Core.Routes;

public interface ITrackService
{
    Task ReplaceTracks(Route route, ReplaceTracksRequest request);

    Task<List<TrackBlueprint>> GetRouteTrackBlueprints(Route route);

    List<string> GetProviders();

    List<string> GetProducts(string provider);

    Task<List<Track>> GetTracks(string providerName, string productName, CancellationToken cancellationToken);
}
