using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using Microsoft.Extensions.DependencyInjection;

using RailworksForge.Core;
using RailworksForge.ViewModels;
using RailworksForge.Views;
using RailworksForge.Views.Controls;
using RailworksForge.Views.Dialogs;

using ConsistDetailPage = RailworksForge.Views.Pages.ConsistDetailPage;
using ReplaceConsistDialog = RailworksForge.Views.Dialogs.ReplaceConsistDialog;
using RouteDetailPage = RailworksForge.Views.Pages.RouteDetailPage;
using RoutesPage = RailworksForge.Views.Pages.RoutesPage;
using SaveConsistDialog = RailworksForge.Views.Dialogs.SaveConsistDialog;
using ScenarioDetailPage = RailworksForge.Views.Pages.ScenarioDetailPage;

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
        ViewLocator.Register<ConsistDetailViewModel, ConsistDetailPage>();
        ViewLocator.Register<NavigationBarViewModel, NavigationBar>();
        ViewLocator.Register<ReplaceConsistViewModel, ReplaceConsistDialog>();
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

        services.AddTransient<RoutesViewModel>();
        services.AddTransient<RoutesBaseViewModel>();

        services.AddTransient<RouteService>();
    }
}
