using System.Collections.Generic;
using System.Linq;

using RailworksForge.Core.Config;

namespace RailworksForge.Services;

public class RecentRoutesService(ISettingsService settings) : IRecentRoutesService
{
    private const int MaxRecentRoutes = 6;

    public IReadOnlyList<string> GetRecentRouteIds()
    {
        return settings.Current.RecentRoutes;
    }

    public void Record(string routeId)
    {
        settings.Update(current =>
        {
            var others = current.RecentRoutes.Where(id => id != routeId);
            var recent = others.Prepend(routeId).Take(MaxRecentRoutes).ToList();

            return current with { RecentRoutes = recent };
        });
    }
}
