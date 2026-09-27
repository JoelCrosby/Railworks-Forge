using Microsoft.Extensions.DependencyInjection;

using RailworksForge.Services;
using RailworksForge.ViewModels;

namespace RailworksForge.UnitTests;

public class NavigationBarTests
{
    [Fact]
    public void OpeningSettings_LeavesEveryCrumbClickable()
    {
        var services = new ServiceCollection();
        App.RegisterServices(services);
        using var provider = services.BuildServiceProvider();
        var navigation = provider.GetRequiredService<NavigationService>();
        var navigationBar = provider.GetRequiredService<NavigationBarViewModel>();

        Assert.True(navigationBar.IsRoutesCrumbCurrent);

        navigation.ShowSettings();

        Assert.False(navigationBar.IsRoutesCrumbCurrent);
        Assert.False(navigationBar.IsRouteCrumbCurrent);
        Assert.False(navigationBar.IsScenarioCrumbCurrent);
        Assert.False(navigationBar.IsConsistCrumbCurrent);
    }
}
