using RailworksForge.Core;

namespace RailworksForge.UnitTests;

public class PathsTests
{
    [Fact]
    public void Exists_ReturnsExpected()
    {
        var result = Paths.Exists("\\JustTrains\\NL\\stock\\Class166\\sr\\dmocl", Paths.GetAssetsDirectory());

        Assert.True(result);
    }
}
