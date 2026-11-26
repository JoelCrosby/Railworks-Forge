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
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IGameDirectoryService, GameDirectoryService>();

        services.AddSingleton<IAssetDirectoryTreeService, AssetDirectoryTreeService>();
        services.AddSingleton<AssetImageService>();
        services.AddSingleton<IScenarioDatabaseService, ScenarioDatabaseService>();
        services.AddSingleton<IScenarioService, ScenarioService>();
        services.AddSingleton<IRouteService, RouteService>();
        services.AddSingleton<ITrackService, TrackService>();
        services.AddSingleton<IRouteAssetCheckService, RouteAssetCheckService>();
        services.AddSingleton<IRollingStockService, RollingStockService>();
        services.AddSingleton<IVehicleIndexService, VehicleIndexService>();
        services.AddSingleton<IPreloadConsistService, PreloadConsistService>();
        services.AddSingleton<ISavedConsistService, SavedConsistService>();
        services.AddSingleton<ISerzFileService, SerzFileService>();
        services.AddSingleton<IPackageInstallService, PackageInstallService>();

        return services;
    }
}
