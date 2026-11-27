namespace RailworksForge.Core.Routes;

public interface IRouteService
{
    event Action? RoutesInvalidated;

    void InvalidateRoutes();

    Task<List<Route>> GetRoutes();

    Task<RouteDetails> GetDetails(Route route);

    Task<Route> UpdateDetails(Route route, RouteDetails details);
}
