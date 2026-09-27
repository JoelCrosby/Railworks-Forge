using System.Globalization;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using Echoes;

using Microsoft.Extensions.DependencyInjection;

using RailworksForge.Core;
using RailworksForge.Core.Config;
using RailworksForge.Core.Packaging;
using RailworksForge.Services;
using RailworksForge.ViewModels;
using RailworksForge.Views;
using RailworksForge.Views.Dialogs;
using RailworksForge.Views.Pages;

namespace RailworksForge;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        var config = Configuration.Get();

        TranslationProvider.SetCulture(CultureInfo.GetCultureInfo(config.Language));

        new ThemeService().Apply(config.Theme);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Paths.SetGameDirectory();

        var services = new ServiceCollection();

        RegisterServices(services);
        RegisterViews();

        var provider = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = provider.GetRequiredService<MainWindowViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<AssetDirectoryTreeService>();
        services.AddSingleton<ScenarioDatabaseService>();
        services.AddSingleton<ScenarioService>();
        services.AddSingleton<RouteService>();
        services.AddSingleton<TrackService>();
        services.AddSingleton<RollingStockService>();
        services.AddSingleton<PreloadConsistService>();
        services.AddSingleton<ConsistEditService>();
        services.AddSingleton<RouteAssetCheckService>();
        services.AddSingleton<SerzFileService>();
        services.AddSingleton<SettingsService>();
        services.AddTransient<Packager>();

        services.AddSingleton<NavigationService>();
        services.AddSingleton<DialogService>();
        services.AddSingleton<LauncherService>();
        services.AddSingleton<ClipboardService>();
        services.AddSingleton<StoragePickerService>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<AppLifetimeService>();
        services.AddSingleton<ToolsActivity>();
        services.AddSingleton<ImageService>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<ToolsMenuViewModel>();
        services.AddSingleton<NavigationBarViewModel>();
        services.AddSingleton<StatusBarViewModel>();
        services.AddSingleton<RoutesViewModel>();
        services.AddTransient<SettingsViewModel>();
    }

    private static void RegisterViews()
    {
        ViewLocator.Register<RoutesViewModel, RoutesPage>();
        ViewLocator.Register<RouteDetailViewModel, RouteDetailPage>();
        ViewLocator.Register<ScenarioDetailViewModel, ScenarioDetailPage>();
        ViewLocator.Register<ConsistDetailViewModel, ConsistDetailPage>();
        ViewLocator.Register<SettingsViewModel, SettingsPage>();

        ViewLocator.Register<CheckAssetsViewModel, CheckAssetsDialog>();
        ViewLocator.Register<ConfirmationDialogViewModel, ConfirmationDialog>();
        ViewLocator.Register<ReplaceConsistViewModel, ReplaceConsistDialog>();
        ViewLocator.Register<ReplaceTrackViewModel, ReplaceTrackDialog>();
        ViewLocator.Register<SaveConsistViewModel, SaveConsistDialog>();
    }
}
