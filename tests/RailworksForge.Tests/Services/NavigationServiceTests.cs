using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using RailworksForge.Core.Config;
using RailworksForge.Core.Routes;
using RailworksForge.Services;
using RailworksForge.Tests.Support;
using RailworksForge.ViewModels;

namespace RailworksForge.Tests.Services;

public class NavigationServiceTests
{
    private readonly NavigationService _navigation;

    public NavigationServiceTests()
    {
        var settings = Substitute.For<ISettingsService>();
        var routes = Substitute.For<IRouteService>();
        var services = new ServiceCollection();

        settings.Current.Returns(new ConfigurationOptions { Language = "en-GB", Theme = "Dark" });
        routes.GetRoutes().Returns([TestData.Route()]);

        services.AddSingleton(settings);
        services.AddSingleton(routes);
        services.AddSingleton(Fakes.ImmediateDispatcher());
        services.AddSingleton(Substitute.For<IImageService>());
        services.AddSingleton(Substitute.For<ILauncherService>());
        services.AddSingleton(Substitute.For<IClipboardService>());
        services.AddSingleton(Substitute.For<IRecentRoutesService>());
        services.AddSingleton(Substitute.For<IThemeService>());
        services.AddSingleton(Substitute.For<IStoragePickerService>());
        services.AddSingleton(Substitute.For<IAppLifetimeService>());
        services.AddSingleton(Substitute.For<IGameDirectoryService>());
        services.AddSingleton(new AppDirectories());
        services.AddSingleton<INavigationService>(provider => provider.GetRequiredService<NavigationService>());
        services.AddSingleton<NavigationService>();
        services.AddTransient<RoutesViewModel>();
        services.AddTransient<SettingsViewModel>();

        _navigation = services.BuildServiceProvider().GetRequiredService<NavigationService>();
    }

    [Fact]
    public void CloseSettings_ReturnsToThePageSettingsWasOpenedFrom()
    {
        _navigation.ShowRoutes();
        var routesPage = _navigation.CurrentPage;
        _navigation.ShowSettings();

        _navigation.CloseSettings();

        Assert.Same(routesPage, _navigation.CurrentPage);
        Assert.False(_navigation.IsSettingsActive);
    }

    [Fact]
    public void CloseSettings_ShowsTheRoutesWhenNothingWasOpenBefore()
    {
        _navigation.ShowSettings();

        _navigation.CloseSettings();

        Assert.IsType<RoutesViewModel>(_navigation.CurrentPage);
    }

    [Fact]
    public void CloseSettings_DoesNothingOutsideSettings()
    {
        _navigation.ShowRoutes();
        var routesPage = _navigation.CurrentPage;

        _navigation.CloseSettings();

        Assert.Same(routesPage, _navigation.CurrentPage);
    }

    [Fact]
    public void ShowSettings_WhileInSettings_KeepsThePageToReturnTo()
    {
        _navigation.ShowRoutes();
        var routesPage = _navigation.CurrentPage;
        _navigation.ShowSettings();
        _navigation.ShowSettings();

        _navigation.CloseSettings();

        Assert.Same(routesPage, _navigation.CurrentPage);
    }
}
