namespace RailworksForge.Core.Routes;

public interface IRouteService
{
    event Action? RoutesInvalidated;

    void InvalidateRoutes();

    Task<List<Route>> GetRoutes();
}
