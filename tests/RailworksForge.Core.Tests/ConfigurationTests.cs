using System.Text.Json;

using RailworksForge.Core.Config;

namespace RailworksForge.Core.Tests;

public class ConfigurationTests
{
    [Fact]
    public void MissingSetting_DefaultsToInternalSerz()
    {
        var options = JsonSerializer.Deserialize<ConfigurationOptions>("{}", Configuration.JsonSerializerOptions);

        Assert.NotNull(options);
        Assert.True(options.UseInternalSerz);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SerzPreference_SurvivesSettingsSerialization(bool useInternalSerz)
    {
        var options = new ConfigurationOptions { UseInternalSerz = useInternalSerz };
        var json = JsonSerializer.Serialize(options, Configuration.JsonSerializerOptions);
        var restored = JsonSerializer.Deserialize<ConfigurationOptions>(json, Configuration.JsonSerializerOptions);

        Assert.NotNull(restored);
        Assert.Equal(useInternalSerz, restored.UseInternalSerz);
    }
}
