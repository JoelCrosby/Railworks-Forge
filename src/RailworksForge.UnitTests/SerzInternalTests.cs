using System.Globalization;
using System.Xml.Linq;

using AngleSharp.Xml;
using AngleSharp.Xml.Parser;

using RailworksForge.Core.External;

namespace RailworksForge.UnitTests;

public class SerzInternalTests
{
    [Theory]
    [InlineData("ScenarioNetworkProperties.bin")]
    [InlineData("Scenario.bin")]
    [InlineData("+000000+000000.bin")]
    [InlineData("ScenaryItem.bin")]
    [InlineData("SerzTypes.bin")]
    [InlineData("SerzNodes.bin")]
    [InlineData("SerzCache.bin")]
    [InlineData("SerzFeatures.bin")]
    [InlineData("SerzFloats.bin")]
    public void Conversion_MatchesSerzOutput(string name)
    {
        var data = GetResourceBytes(name);
        using var expectedStream = new MemoryStream(GetResourceBytes(name + ".xml"));
        var expected = XDocument.Load(expectedStream);
        var document = new SerzInternal(ref data).ToXml();
        var actual = XDocument.Parse(document.ToXml());

        Assert.Equal(expected.ToString(), actual.ToString());

        var directory = Directory.CreateTempSubdirectory("railworks-serz-");

        try
        {
            var inputPath = Path.Join(directory.FullName, "input.bin");
            var outputPath = Path.Join(directory.FullName, "output.xml");
            File.WriteAllBytes(inputPath, data);
            File.WriteAllText(outputPath, "previous output");
            SerzInternal.Convert(inputPath, outputPath);
            var streamed = XDocument.Load(outputPath);

            Assert.Equal(expected.ToString(), streamed.ToString());
            Assert.Equal(2, Directory.GetFiles(directory.FullName).Length);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Fact]
    public void Conversion_WritesForbiddenCharactersLikeSerz()
    {
        var data = GetResourceBytes("SerzControlChars.bin");
        var expectedXml = GetResourceBytes("SerzControlChars.bin.xml");
        var expected = GetLeafValues(expectedXml);
        var directory = Directory.CreateTempSubdirectory("railworks-serz-");

        try
        {
            var inputPath = Path.Join(directory.FullName, "input.bin");
            var outputPath = Path.Join(directory.FullName, "output.xml");
            File.WriteAllBytes(inputPath, data);
            SerzInternal.Convert(inputPath, outputPath);
            var converted = GetLeafValues(File.ReadAllBytes(outputPath));
            var document = new SerzInternal(ref data).ToXml();
            var parsed = document.QuerySelectorAll("cRoot > *").Select(element => (element.LocalName, element.TextContent));

            Assert.Equal(expected, converted);
            Assert.Equal(expected, parsed);
            Assert.Contains(expected, value => value.Text == "x\0y");
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Fact]
    public void ToXml_IsRepeatableAndCultureIndependent()
    {
        var data = GetResourceBytes("SerzTypes.bin");
        var converter = new SerzInternal(ref data);
        var expected = converter.ToXml().ToXml();
        var previousCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var actual = converter.ToXml().ToXml();

            Assert.Equal(expected, actual);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public void ToXml_RejectsInvalidHeader()
    {
        var data = new byte[8];

        Assert.Throws<InvalidDataException>(() => new SerzInternal(ref data).ToXml());
    }

    [Fact]
    public void ToXml_RejectsTruncatedInput()
    {
        var complete = GetResourceBytes("SerzNodes.bin");

        for (var length = 0; length < complete.Length; length++)
        {
            var data = complete[..length];

            Assert.Throws<InvalidDataException>(() => new SerzInternal(ref data).ToXml());
        }
    }

    [Theory]
    [InlineData("00")]
    [InlineData("FF5A")]
    [InlineData("FF500000")]
    [InlineData("FF42FFFFFFFF")]
    [InlineData("FF50FFFF01000000FF")]
    public void ToXml_RejectsInvalidChunks(string payload)
    {
        byte[] data = [.. "SERZ\0\0\x01\0"u8, .. Convert.FromHexString(payload)];
        var exception = Assert.Throws<InvalidDataException>(() => new SerzInternal(ref data).ToXml());

        Assert.Contains("byte offset", exception.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Convert_FailurePreservesExistingOutput(bool cancel)
    {
        var directory = Directory.CreateTempSubdirectory("railworks-serz-");

        try
        {
            var inputPath = Path.Join(directory.FullName, "input.bin");
            var outputPath = Path.Join(directory.FullName, "output.xml");
            var data = GetResourceBytes("SerzNodes.bin");
            File.WriteAllBytes(inputPath, data[..^1]);
            File.WriteAllText(outputPath, "previous output");
            using var cancellation = new CancellationTokenSource();

            if (cancel)
            {
                cancellation.Cancel();
                Assert.Throws<OperationCanceledException>(() =>
                    SerzInternal.Convert(inputPath, outputPath, cancellation.Token));
            }
            else
            {
                Assert.Throws<InvalidDataException>(() => SerzInternal.Convert(inputPath, outputPath));
            }

            Assert.Equal("previous output", File.ReadAllText(outputPath));
            Assert.Equal(2, Directory.GetFiles(directory.FullName).Length);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    private static List<(string Name, string Text)> GetLeafValues(byte[] xml)
    {
        using var stream = new MemoryStream(xml);
        var document = new XmlParser().ParseDocument(stream);

        return document.QuerySelectorAll("cRoot > *").Select(element => (element.LocalName, element.TextContent)).ToList();
    }

    private static byte[] GetResourceBytes(string name)
    {
        var assembly = typeof(SerzInternalTests).Assembly;
        using var resource = assembly.GetManifestResourceStream($"RailworksForge.UnitTests.Resources.{name}");

        if (resource is null)
        {
            throw new Exception($"Could not find resource {name}");
        }

        using var buffer = new MemoryStream();
        resource.CopyTo(buffer);

        return buffer.ToArray();
    }
}
