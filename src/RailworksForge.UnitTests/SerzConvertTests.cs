using System.Xml.Linq;

using RailworksForge.Core.External;

namespace RailworksForge.UnitTests;

public sealed class SerzConvertTests : IDisposable
{
    private const string NativeOutput = "native output";
    private const string CachedOutput = "cached output";

    private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("railworks-serz-convert-");
    private readonly List<(string InputPath, string OutputPath)> _nativeCalls = [];

    public void Dispose()
    {
        _directory.Delete(true);
    }

    [Fact]
    public async Task BinaryInput_WithInternalSerz_ConvertsWithoutGameSerz()
    {
        var conversion = CreateConversion("Scenario.bin", "Scenario.bin.xml", useInternalSerz: true);
        File.WriteAllBytes(conversion.InputPath, GetResourceBytes("SerzNodes.bin"));

        var result = await Serz.Convert(conversion, RecordNativeCall, CancellationToken.None);
        var expected = XDocument.Parse(GetResourceText("SerzNodes.bin.xml"));
        var actual = XDocument.Load(result.OutputPath);

        Assert.Empty(_nativeCalls);
        Assert.Equal(conversion.OutputPath, result.OutputPath);
        Assert.Equal(expected.ToString(), actual.ToString());
    }

    [Fact]
    public async Task BinaryInput_WithoutInternalSerz_ConvertsWithGameSerz()
    {
        var conversion = CreateConversion("Scenario.bin", "Scenario.bin.xml", useInternalSerz: false);
        File.WriteAllBytes(conversion.InputPath, GetResourceBytes("SerzNodes.bin"));

        var result = await Serz.Convert(conversion, RecordNativeCall, CancellationToken.None);
        var nativeCall = Assert.Single(_nativeCalls);

        Assert.Equal(conversion.InputPath, nativeCall.InputPath);
        Assert.NotEqual(conversion.OutputPath, nativeCall.OutputPath);
        Assert.Equal(NativeOutput, File.ReadAllText(result.OutputPath));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task XmlInput_AlwaysConvertsWithGameSerz(bool useInternalSerz)
    {
        var conversion = CreateConversion("Scenario.bin.xml", "Scenario.bin", useInternalSerz);
        File.WriteAllText(conversion.InputPath, "<cRoot />");

        var result = await Serz.Convert(conversion, RecordNativeCall, CancellationToken.None);

        Assert.Single(_nativeCalls);
        Assert.Equal(NativeOutput, File.ReadAllText(result.OutputPath));
    }

    [Fact]
    public async Task CurrentCache_IsReturnedWithoutConverting()
    {
        var conversion = CreateConversion("Scenario.bin", "Scenario.bin.xml", useInternalSerz: false);
        WriteCachedOutput(conversion, isCacheCurrent: true);

        var result = await Serz.Convert(conversion, RecordNativeCall, CancellationToken.None);

        Assert.Empty(_nativeCalls);
        Assert.Equal(CachedOutput, File.ReadAllText(result.OutputPath));
    }

    [Fact]
    public async Task StaleCache_IsConvertedAgain()
    {
        var conversion = CreateConversion("Scenario.bin", "Scenario.bin.xml", useInternalSerz: false);
        WriteCachedOutput(conversion, isCacheCurrent: false);

        var result = await Serz.Convert(conversion, RecordNativeCall, CancellationToken.None);

        Assert.Single(_nativeCalls);
        Assert.Equal(NativeOutput, File.ReadAllText(result.OutputPath));
    }

    [Fact]
    public async Task Force_ConvertsAgainWhenCacheIsCurrent()
    {
        var conversion = CreateConversion("Scenario.bin", "Scenario.bin.xml", useInternalSerz: false) with { Force = true };
        WriteCachedOutput(conversion, isCacheCurrent: true);

        var result = await Serz.Convert(conversion, RecordNativeCall, CancellationToken.None);

        Assert.Single(_nativeCalls);
        Assert.Equal(NativeOutput, File.ReadAllText(result.OutputPath));
    }

    [Fact]
    public async Task GameSerzWithoutOutput_FailsAndKeepsCachedOutput()
    {
        var conversion = CreateConversion("Scenario.bin", "Scenario.bin.xml", useInternalSerz: false) with { Force = true };
        WriteCachedOutput(conversion, isCacheCurrent: true);

        var exception = await Assert.ThrowsAsync<Exception>(() =>
            Serz.Convert(conversion, (_, _, _) => Task.CompletedTask, CancellationToken.None));

        Assert.Equal("Serz execution failed", exception.Message);
        AssertOnlyInputAndCachedOutputRemain(conversion);
    }

    [Fact]
    public async Task GameSerzFailure_KeepsCachedOutputAndRemovesPartialOutput()
    {
        var conversion = CreateConversion("Scenario.bin", "Scenario.bin.xml", useInternalSerz: false) with { Force = true };
        WriteCachedOutput(conversion, isCacheCurrent: true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Serz.Convert(conversion, WritePartialOutputAndFail, CancellationToken.None));

        AssertOnlyInputAndCachedOutputRemain(conversion);
    }

    [Fact]
    public async Task InternalSerzFailure_KeepsCachedOutput()
    {
        var conversion = CreateConversion("Scenario.bin", "Scenario.bin.xml", useInternalSerz: true) with { Force = true };
        WriteCachedOutput(conversion, isCacheCurrent: true);
        File.WriteAllBytes(conversion.InputPath, GetResourceBytes("SerzNodes.bin")[..^1]);

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            Serz.Convert(conversion, RecordNativeCall, CancellationToken.None));

        Assert.Empty(_nativeCalls);
        AssertOnlyInputAndCachedOutputRemain(conversion);
    }

    [Fact]
    public async Task CancelledConversion_DoesNotConvert()
    {
        var conversion = CreateConversion("Scenario.bin", "Scenario.bin.xml", useInternalSerz: false);
        File.WriteAllBytes(conversion.InputPath, GetResourceBytes("SerzNodes.bin"));
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Serz.Convert(conversion, RecordNativeCall, cancellation.Token));

        Assert.Empty(_nativeCalls);
        Assert.False(File.Exists(conversion.OutputPath));
    }

    private Serz.SerzConversion CreateConversion(string inputName, string outputName, bool useInternalSerz)
    {
        var inputDirectory = _directory.CreateSubdirectory("game");
        var outputDirectory = _directory.CreateSubdirectory("cache");

        return new Serz.SerzConversion
        {
            InputPath = Path.Join(inputDirectory.FullName, inputName),
            OutputPath = Path.Join(outputDirectory.FullName, outputName),
            UseInternalSerz = useInternalSerz,
        };
    }

    private static void WriteCachedOutput(Serz.SerzConversion conversion, bool isCacheCurrent)
    {
        var inputWrittenAt = DateTime.UtcNow.AddHours(-1);
        var cacheOffset = isCacheCurrent ? TimeSpan.FromMinutes(1) : TimeSpan.FromMinutes(-1);
        var outputWrittenAt = inputWrittenAt + cacheOffset;

        File.WriteAllBytes(conversion.InputPath, GetResourceBytes("SerzNodes.bin"));
        File.WriteAllText(conversion.OutputPath, CachedOutput);
        File.SetLastWriteTimeUtc(conversion.InputPath, inputWrittenAt);
        File.SetLastWriteTimeUtc(conversion.OutputPath, outputWrittenAt);
    }

    private static void AssertOnlyInputAndCachedOutputRemain(Serz.SerzConversion conversion)
    {
        var outputDirectory = Path.GetDirectoryName(conversion.OutputPath)!;
        var outputFiles = Directory.GetFiles(outputDirectory);

        Assert.True(File.Exists(conversion.InputPath));
        Assert.Equal([conversion.OutputPath], outputFiles);
        Assert.Equal(CachedOutput, File.ReadAllText(conversion.OutputPath));
    }

    private Task RecordNativeCall(string inputPath, string outputPath, CancellationToken token)
    {
        _nativeCalls.Add((inputPath, outputPath));
        File.WriteAllText(outputPath, NativeOutput);

        return Task.CompletedTask;
    }

    private static Task WritePartialOutputAndFail(string inputPath, string outputPath, CancellationToken token)
    {
        File.WriteAllText(outputPath, "partial");

        throw new InvalidOperationException("serz64.exe exited with an error");
    }

    private static string GetResourceText(string name)
    {
        using var stream = new MemoryStream(GetResourceBytes(name));
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }

    private static byte[] GetResourceBytes(string name)
    {
        var assembly = typeof(SerzConvertTests).Assembly;
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
