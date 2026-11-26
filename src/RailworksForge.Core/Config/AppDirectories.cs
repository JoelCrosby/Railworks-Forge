using RailworksForge.Core.Common;

namespace RailworksForge.Core.Config;

public class AppDirectories
{
    public string Configuration => Paths.GetConfigurationFolder();

    public string Logs => Paths.GetLoggingPath();

    public string Cache => Paths.GetCacheFolder();
}
