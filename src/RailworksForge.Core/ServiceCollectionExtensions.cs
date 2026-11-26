using Microsoft.Extensions.DependencyInjection;

using RailworksForge.Core.Assets;
using RailworksForge.Core.Config;
using RailworksForge.Core.Consists;
using RailworksForge.Core.Packaging;
using RailworksForge.Core.Routes;
using RailworksForge.Core.Scenarios;
using RailworksForge.Core.Serialization;
using RailworksForge.Core.Vehicles;

namespace RailworksForge.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRailworksForgeCore(this IServiceCollection services)
    {
        services.AddSingleton<AppDirectories>();
        services.AddSingleton<SettingsService>();
        services.AddSingleton<GameDirectoryService>();

        services.AddSingleton<AssetDirectoryTreeService>();
        services.AddSingleton<AssetImageService>();
        services.AddSingleton<ScenarioDatabaseService>();
        services.AddSingleton<ScenarioService>();
        services.AddSingleton<RouteService>();
        services.AddSingleton<TrackService>();
        services.AddSingleton<RouteAssetCheckService>();
        services.AddSingleton<RollingStockService>();
        services.AddSingleton<VehicleIndexService>();
        services.AddSingleton<PreloadConsistService>();
        services.AddSingleton<SavedConsistService>();
        services.AddSingleton<SerzFileService>();
        services.AddSingleton<PackageInstallService>();

        return services;
    }
}
