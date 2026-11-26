namespace RailworksForge.Core.Config;

public interface ISettingsService
{
    ConfigurationOptions Current { get; }

    void Update(Func<ConfigurationOptions, ConfigurationOptions> change);
}
