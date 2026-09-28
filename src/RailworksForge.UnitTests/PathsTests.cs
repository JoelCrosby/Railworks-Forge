using RailworksForge.Core;

namespace RailworksForge.UnitTests;

public class PathsTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("railworks-paths-");

    public PathsTests()
    {
        var product = Directory.CreateDirectory(Path.Join(_root.FullName, "Assets", "Kuju", "RailSimulator"));
        File.WriteAllText(Path.Join(product.FullName, "Blueprint.bin"), string.Empty);
    }

    public void Dispose()
    {
        _root.Delete(recursive: true);
    }

    [Theory]
    [InlineData("Assets/Kuju/RailSimulator/Blueprint.bin")]
    [InlineData("assets/KUJU/railsimulator/blueprint.BIN")]
    [InlineData(@"Assets\kuju\RailSimulator\Blueprint.bin")]
    public void GetActualPathFromInsensitive_ResolvesAnyCase(string relativePath)
    {
        var expected = Path.Join(_root.FullName, "Assets", "Kuju", "RailSimulator", "Blueprint.bin");

        var withRoot = Paths.GetActualPathFromInsensitive(Path.Join(_root.FullName, relativePath), _root.FullName);
        var withoutRoot = Paths.GetActualPathFromInsensitive(Path.Join(_root.FullName, relativePath));

        Assert.Equal(expected, withRoot);
        Assert.Equal(expected, withoutRoot);
    }

    [Fact]
    public void GetActualPathFromInsensitive_ReturnsNullForMissingPath()
    {
        var missing = Path.Join(_root.FullName, "assets", "kuju", "Missing", "Blueprint.bin");

        Assert.Null(Paths.GetActualPathFromInsensitive(missing, _root.FullName));
    }

    [Fact]
    public void GetActualPathFromInsensitive_IgnoresRootThatIsNotAPrefix()
    {
        var path = Path.Join(_root.FullName, "assets", "kuju", "railsimulator", "blueprint.bin");
        var unrelatedRoot = Path.Join(_root.FullName, "Assets", "Other");
        var expected = Path.Join(_root.FullName, "Assets", "Kuju", "RailSimulator", "Blueprint.bin");

        Assert.Equal(expected, Paths.GetActualPathFromInsensitive(path, unrelatedRoot));
    }
}
