using System.Globalization;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

using Echoes;

using Microsoft.Extensions.DependencyInjection;

using RailworksForge.Core;
using RailworksForge.Core.Config;
using RailworksForge.ViewModels;
using RailworksForge.Views;
using RailworksForge.Views.Controls;
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

        RequestedThemeVariant = config.Theme switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Paths.SetGameDirectory();

        var services = new ServiceCollection();

        RegisterServices(services);
        RegisterViews();

        var provider = services.BuildServiceProvider();
        var dataContext = provider.GetRequiredService<MainWindowViewModel>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = dataContext,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void RegisterViews()
    {
        ViewLocator.Register<MainWindowViewModel, MainWindow>();
        ViewLocator.Register<MainMenuViewModel, MainMenu>();
        ViewLocator.Register<ConsistDetailViewModel, ConsistDetailPage>();
        ViewLocator.Register<NavigationBarViewModel, NavigationBar>();
        ViewLocator.Register<ReplaceConsistViewModel, ReplaceConsistDialog>();
        ViewLocator.Register<ReplaceTrackViewModel, ReplaceTrackDialog>();
        ViewLocator.Register<CheckAssetsViewModel, CheckAssetsDialog>();
        ViewLocator.Register<RouteDetailViewModel, RouteDetailPage>();
        ViewLocator.Register<RoutesViewModel, RoutesPage>();
        ViewLocator.Register<RoutesBaseViewModel, RoutesList>();
        ViewLocator.Register<ConfirmationDialogViewModel, ConfirmationDialog>();
        ViewLocator.Register<SaveConsistViewModel, SaveConsistDialog>();
        ViewLocator.Register<ScenarioDetailViewModel, ScenarioDetailPage>();
        ViewLocator.Register<StatusBarViewModel, StatusBar>();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<MainMenuViewModel>();
        services.AddTransient<ConsistDetailViewModel>();
        services.AddTransient<NavigationBarViewModel>();
        services.AddTransient<ReplaceConsistViewModel>();
        services.AddTransient<ReplaceTrackViewModel>();
        services.AddTransient<CheckAssetsViewModel>();
        services.AddTransient<RouteDetailViewModel>();
        services.AddTransient<RoutesViewModel>();
        services.AddTransient<RoutesBaseViewModel>();
        services.AddTransient<ConfirmationDialogViewModel>();
        services.AddTransient<SaveConsistViewModel>();
        services.AddTransient<ScenarioDetailViewModel>();
        services.AddTransient<StatusBarViewModel>();
        services.AddTransient<ProgressIndicatorViewModel>();

        services.AddTransient<RouteService>();

        services.AddSingleton<AssetDirectoryTreeService>();
    }
}
