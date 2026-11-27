using NSubstitute;

using RailworksForge.Core.Config;
using RailworksForge.Services;

namespace RailworksForge.Tests.Services;

public class RecentRoutesServiceTests
{
    private readonly ISettingsService _settings = Substitute.For<ISettingsService>();
    private readonly RecentRoutesService _recentRoutes;
    private ConfigurationOptions _config = new();

    public RecentRoutesServiceTests()
    {
        _settings.Current.Returns(_ => _config);
        _settings
            .When(settings => settings.Update(Arg.Any<Func<ConfigurationOptions, ConfigurationOptions>>()))
            .Do(call => _config = call.Arg<Func<ConfigurationOptions, ConfigurationOptions>>().Invoke(_config));

        _recentRoutes = new RecentRoutesService(_settings);
    }

    [Fact]
    public void Record_PutsTheRouteFirst()
    {
        _recentRoutes.Record("a");
        _recentRoutes.Record("b");

        Assert.Equal(["b", "a"], _recentRoutes.GetRecentRouteIds());
    }

    [Fact]
    public void Record_MovesAReopenedRouteToTheFrontWithoutDuplicating()
    {
        _recentRoutes.Record("a");
        _recentRoutes.Record("b");
        _recentRoutes.Record("a");

        Assert.Equal(["a", "b"], _recentRoutes.GetRecentRouteIds());
    }

    [Fact]
    public void Record_KeepsTheSixMostRecentRoutes()
    {
        string[] opened = ["1", "2", "3", "4", "5", "6", "7"];

        foreach (var id in opened)
        {
            _recentRoutes.Record(id);
        }

        Assert.Equal(["7", "6", "5", "4", "3", "2"], _recentRoutes.GetRecentRouteIds());
    }
}
