using RailworksForge.Core.External;

namespace RailworksForge.UnitTests;

public class SerzInternalTests
{
    [Fact]
    public void SerzInternal_ScenarioNetworkProperties_CreatesDocument()
    {
        var data = GetResourceStream("ScenarioNetworkProperties.bin");
        var document = new SerzInternal(ref data).ToXml();

        Assert.NotNull(document.DocumentElement);
    }

    [Fact]
    public void SerzInternal_Scenario_CreatesDocument()
    {
        var data = GetResourceStream("Scenario.bin");
        var document = new SerzInternal(ref data).ToXml();

        Assert.NotNull(document.DocumentElement);
    }

    private static byte[] GetResourceStream(string name)
    {
        var assembly = typeof(SerzInternalTests).Assembly;

        using var resource = assembly.GetManifestResourceStream($"RailworksForge.UnitTests.Resources.{name}");

        if (resource is null)
        {
            throw new Exception($"could not resource in assembly with name {name}");
        }

        var buffer = new byte[resource.Length];
        _ = resource.Read(buffer, 0, buffer.Length);

        return buffer;
    }
}
