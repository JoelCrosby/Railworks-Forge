using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using Microsoft.Extensions.DependencyInjection;

using RailworksForge.Core;
using RailworksForge.ViewModels;
using RailworksForge.Views;
using RailworksForge.Views.Controls;

namespace RailworksForge;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
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
        ViewLocator.Register<ConsistDetailViewModel, ConsistDetail>();
        ViewLocator.Register<FileBrowserViewModel, FileBrowser>();
        ViewLocator.Register<NavigationBarViewModel, NavigationBar>();
        ViewLocator.Register<ReplaceConsistViewModel, ReplaceConsistDialog>();
        ViewLocator.Register<RouteDetailViewModel, RouteDetail>();
        ViewLocator.Register<RoutesViewModel, Routes>();
        ViewLocator.Register<RoutesBaseViewModel, RoutesList>();
        ViewLocator.Register<SaveConsistViewModel, SaveConsistDialog>();
        ViewLocator.Register<ScenarioDetailViewModel, ScenarioDetail>();
        ViewLocator.Register<StatusBarViewModel, StatusBar>();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<MainMenuViewModel>();

        services.AddTransient<RoutesViewModel>();
        services.AddTransient<RoutesBaseViewModel>();

        services.AddTransient<RouteService>();
    }
}
