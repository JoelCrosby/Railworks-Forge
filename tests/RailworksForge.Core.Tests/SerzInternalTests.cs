using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

using AngleSharp.Xml;
using AngleSharp.Xml.Parser;

using RailworksForge.Core.Serialization;

namespace RailworksForge.Core.Tests;

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
    [InlineData("SerzNaN.bin")]
    [InlineData("SerzArrays.bin")]
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Conversion_DuplicateClosingElementReportsFileAndElementNames(bool writeFile)
    {
        var directory = Directory.CreateTempSubdirectory("railworks-serz-");

        try
        {
            var inputPath = Path.Join(directory.FullName, "TrackRules.bin");
            var outputPath = Path.Join(directory.FullName, "TrackRules.xml");
            File.WriteAllBytes(inputPath, GetResourceBytes("SerzDuplicateClose.bin"));
            File.WriteAllText(outputPath, "previous output");
            var exception = Assert.Throws<InvalidDataException>(() =>
            {

                if (writeFile)
                {
                    SerzInternal.Convert(inputPath, outputPath);
                }
                else
                {
                    using var document = SerzInternal.Convert(inputPath);
                }
            });

            Assert.Contains(inputPath, exception.Message);
            Assert.Contains("AutomaticJunctionEntity", exception.Message);
            Assert.Contains("expected 'cTrackRulesBlueprint'", exception.Message);
            Assert.Contains("SERZ byte offset", exception.Message);
            Assert.IsType<InvalidDataException>(exception.InnerException);
            Assert.Equal("previous output", File.ReadAllText(outputPath));
            Assert.Equal(2, Directory.GetFiles(directory.FullName).Length);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Fact]
    public void ToXml_WritesEmptyArray()
    {
        var emptyArray = ArrayRecord("empty", "sInt32");
        var data = BuildSerz(OpenRecord("cRoot"), emptyArray, CloseRecord("cRoot"));
        var document = XDocument.Parse(new SerzInternal(ref data).ToXml().ToXml());
        var element = Assert.Single(document.Root!.Elements());
        var numElements = element.Attribute(Delta + "numElements")?.Value;

        Assert.Equal("empty", element.Name.LocalName);
        Assert.Equal("0", numElements);
        Assert.Equal(string.Empty, element.Value);
    }

    // serz64.exe reads string arrays but writes every element as an empty string, so there is no golden XML.
    [Fact]
    public void ToXml_KeepsStringArrayValues()
    {
        var stringArray = ArrayRecord("strings", "cDeltaString", NewString("alpha"), NewString("b & <c>"), NewString("ünï"));
        var data = BuildSerz(OpenRecord("cRoot"), stringArray, CloseRecord("cRoot"));
        var document = XDocument.Parse(new SerzInternal(ref data).ToXml().ToXml());
        var element = Assert.Single(document.Root!.Elements());

        Assert.Equal("alpha b & <c> ünï", element.Value);
    }

    [Fact]
    public void ToXml_RejectsUnsupportedValueType()
    {
        var unsupportedValue = ValueRecord("value", "sBogus", [0x01]);
        var data = BuildSerz(OpenRecord("cRoot"), unsupportedValue, CloseRecord("cRoot"));
        var exception = Assert.Throws<InvalidDataException>(() => new SerzInternal(ref data).ToXml());

        Assert.Contains("Unsupported value type 'sBogus'", exception.Message);
    }

    [Fact]
    public void ToXml_RejectsCloseWithoutOpenElement()
    {
        var data = BuildSerz(OpenRecord("cRoot"), CloseRecord("cRoot"), CloseRecord("cRoot"));
        var exception = Assert.Throws<InvalidDataException>(() => new SerzInternal(ref data).ToXml());

        Assert.Contains("Mismatched closing element 'cRoot' (expected '<no open element>')", exception.Message);
    }

    [Fact]
    public void ToXml_KeepsSurrogatePairsInTextWithForbiddenCharacters()
    {
        const string text = "\U0001F682\0x";
        var value = ValueRecord("value", "cDeltaString", NewString(text));
        var data = BuildSerz(OpenRecord("cRoot"), value, CloseRecord("cRoot"));
        var document = new SerzInternal(ref data).ToXml();
        var element = Assert.Single(document.QuerySelectorAll("cRoot > *"));

        Assert.Equal(text, element.TextContent);
    }

    private static readonly XNamespace Delta = "http://www.kuju.com/TnT/2003/Delta";

    private static byte[] BuildSerz(params byte[][] records)
    {
        return [.. "SERZ\0\0\x01\0"u8, .. records.SelectMany(record => record)];
    }

    private static byte[] OpenRecord(string name)
    {
        return [0xFF, (byte)'P', .. NewString(name), .. new byte[8]];
    }

    private static byte[] CloseRecord(string name)
    {
        return [0xFF, (byte)'p', .. NewString(name)];
    }

    private static byte[] ValueRecord(string name, string type, byte[] value)
    {
        return [0xFF, (byte)'V', .. NewString(name), .. NewString(type), .. value];
    }

    private static byte[] ArrayRecord(string name, string type, params byte[][] values)
    {
        var elements = values.SelectMany(value => value);

        return [0xFF, (byte)'A', .. NewString(name), .. NewString(type), (byte)values.Length, .. elements];
    }

    // SERZ prefixes new strings with a 0xFFFF index marker and a length counted in Unicode characters.
    private static byte[] NewString(string value)
    {
        var length = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(length, (uint)value.EnumerateRunes().Count());

        return [0xFF, 0xFF, .. length, .. Encoding.UTF8.GetBytes(value)];
    }

    private static List<(string Name, string Text)> GetLeafValues(byte[] xml)
    {
        using var stream = new MemoryStream(xml);
        var document = new AngleSharp.Xml.Parser.XmlParser().ParseDocument(stream);

        return document.QuerySelectorAll("cRoot > *").Select(element => (element.LocalName, element.TextContent)).ToList();
    }

    private static byte[] GetResourceBytes(string name)
    {
        var assembly = typeof(SerzInternalTests).Assembly;
        using var resource = assembly.GetManifestResourceStream($"RailworksForge.Core.Tests.Resources.{name}");

        if (resource is null)
        {
            throw new Exception($"Could not find resource {name}");
        }

        using var buffer = new MemoryStream();
        resource.CopyTo(buffer);

        return buffer.ToArray();
    }
}
