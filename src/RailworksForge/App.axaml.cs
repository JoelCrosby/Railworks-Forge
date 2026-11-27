using System.Globalization;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using Echoes;

using Microsoft.Extensions.DependencyInjection;

using RailworksForge.Core;
using RailworksForge.Core.Config;
using RailworksForge.Services;
using RailworksForge.ViewModels;
using RailworksForge.Views.Dialogs;
using RailworksForge.Views.Pages;
using RailworksForge.Views;

using Serilog;

namespace RailworksForge;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        var config = new SettingsService().Current;

        TranslationProvider.SetCulture(CultureInfo.GetCultureInfo(config.Language));

        new ThemeService().ApplyAtStartup(config.Theme);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        RegisterServices(services);
        RegisterViews();

        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IGameDirectoryService>().Resolve();

        Dispatcher.UIThread.UnhandledException += OnUnhandledUiException;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = provider.GetRequiredService<MainWindowViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    // Commands rethrow their exceptions on the UI thread; without this a failed clipboard write or save exits the app.
    private static void OnUnhandledUiException(object? sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unhandled exception on the UI thread");

        e.Handled = true;

        LoadingOperation.ReportUnobservedFailure(e.Exception.Message);
    }

    public static void RegisterServices(IServiceCollection services)
    {
        services.AddRailworksForgeCore();

        services.AddSingleton<IDispatcher>(_ => Dispatcher.UIThread);
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<ILauncherService, LauncherService>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IStoragePickerService, StoragePickerService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IAppLifetimeService, AppLifetimeService>();
        services.AddSingleton<ToolsActivity>();
        services.AddSingleton<IImageService, ImageService>();
        services.AddSingleton<IRecentRoutesService, RecentRoutesService>();

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
        ViewLocator.Register<VehicleIndexDialogViewModel, VehicleIndexDialog>();
        ViewLocator.Register<ReplaceConsistViewModel, ReplaceConsistDialog>();
        ViewLocator.Register<ReplaceTrackViewModel, ReplaceTrackDialog>();
        ViewLocator.Register<SaveConsistViewModel, SaveConsistDialog>();
        ViewLocator.Register<EditScenarioDetailsViewModel, EditScenarioDetailsDialog>();
        ViewLocator.Register<EditRouteDetailsViewModel, EditRouteDetailsDialog>();
    }
}
