using System.IO.Compression;
using System.Text;

using RailworksForge.Core;
using RailworksForge.Core.Packaging;

namespace RailworksForge.UnitTests;

public class PackagingTests
{
    [Theory]
    [InlineData(@"Assets\Kuju\RailSimulator\Blueprint.xml", "Assets/Kuju/RailSimulator/Blueprint.xml")]
    [InlineData("/Content/Routes/Route.xml", "Content/Routes/Route.xml")]
    public void ResolveWithin_ReturnsPathInsideRoot(string relativePath, string expectedRelativePath)
    {
        var root = Path.Join(Path.GetTempPath(), "railworks-root");

        var resolved = Paths.ResolveWithin(root, relativePath);

        Assert.Equal(Path.Join(root, expectedRelativePath), resolved);
    }

    [Theory]
    [InlineData(@"..\..\.bashrc")]
    [InlineData("Assets/../../outside.txt")]
    [InlineData("..")]
    public void ResolveWithin_RejectsPathOutsideRoot(string relativePath)
    {
        var root = Path.Join(Path.GetTempPath(), "railworks-root");

        Assert.Throws<InvalidDataException>(() => Paths.ResolveWithin(root, relativePath));
    }

    [Fact]
    public void OffsetReadStream_ReadsZipAfterHeader()
    {
        using var zipBytes = new MemoryStream();

        using (var zip = new ZipArchive(zipBytes, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = zip.CreateEntry("Assets/file.txt");
            using var writer = new StreamWriter(entry.Open());
            writer.Write("content");
        }

        var header = Encoding.UTF8.GetBytes("header-bytes");
        using var package = new MemoryStream();
        package.Write(header);
        package.Write(zipBytes.ToArray());
        package.Position = header.Length;

        using var archiveStream = new OffsetReadStream(package, package.Position);
        using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read);
        using var reader = new StreamReader(archive.Entries.Single().Open());

        Assert.Equal("content", reader.ReadToEnd());
    }
}
