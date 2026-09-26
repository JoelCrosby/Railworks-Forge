namespace RailworksForge.Core.Config;

public class SettingsService
{
    public ConfigurationOptions Current => Configuration.Get();

    public void Update(Func<ConfigurationOptions, ConfigurationOptions> change)
    {
        var current = Configuration.Get();
        var updated = change(current);

        if (updated == current)
        {
            return;
        }

        Configuration.Set(updated);
    }
}
