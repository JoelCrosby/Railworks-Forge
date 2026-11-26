using RailworksForge.Core.Common;

namespace RailworksForge.Core.Tests;

public class CacheTests
{
    [Fact]
    public void RebuildableGameCacheFiles_NeverIncludesScenarioDatabase()
    {
        Assert.DoesNotContain(Cache.RebuildableGameCacheFiles, file => file.StartsWith("SDBCache", StringComparison.OrdinalIgnoreCase));
    }
}
