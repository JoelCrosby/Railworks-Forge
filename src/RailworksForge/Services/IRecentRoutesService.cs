using System.Collections.Generic;

namespace RailworksForge.Services;

public interface IRecentRoutesService
{
    IReadOnlyList<string> GetRecentRouteIds();

    void Record(string routeId);
}
