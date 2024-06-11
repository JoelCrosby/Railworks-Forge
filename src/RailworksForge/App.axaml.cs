using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using Microsoft.Extensions.DependencyInjection;

using RailworksForge.Core;
using RailworksForge.ViewModels;
using RailworksForge.Views;

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

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<MainMenuViewModel>();
        services.AddTransient<RoutesListViewModel>();

        services.AddTransient<RouteService>();
    }
}
