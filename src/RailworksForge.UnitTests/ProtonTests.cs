using RailworksForge.Core.External;

namespace RailworksForge.UnitTests;

public class ProtonTests
{
    private const string TestBinFile = "/cache/SteamLibrary/steamapps/common/RailWorks/Content/Routes/045911ae-114c-4dfc-8382-4505d0491555/Scenarios/456d3787-359d-4d0a-b339-784efff85b97/Scenario.bin";
    private const string TestAssetBinFile = "/cache/SteamLibrary/steamapps/common/RailWorks/Assets/DTG/Class390Pack01/RailVehicles/Electric/Class390/Default/DMRF/Class390DMRF.bin";
    private const string TestPreLoadAssetBinFile = "/cache/SteamLibrary/steamapps/common/RailWorks/Assets/DTG/Class390Pack01/PreLoad/Class390_9car.bin";

    [Fact]
    public async Task ExecProcess_ConvertToXml()
    {
        var output = await Serz.Convert(TestBinFile);

        Assert.NotNull(output);
    }

    [Fact]
    public async Task ExecProcess_ConvertToBin()
    {
        var output = await Serz.Convert(TestBinFile.Replace(".bin", ".bin.xml"));

        Assert.NotNull(output);
    }

    [Fact]
    public async Task ExecProcess_ConvertAssetToXml()
    {
        var output = await Serz.Convert(TestAssetBinFile);

        Assert.NotNull(output);
    }

    [Fact]
    public async Task ExecProcess_ConvertPreloadAssetToXml()
    {
        var output = await Serz.Convert(TestPreLoadAssetBinFile);

        Assert.NotNull(output);
    }
}
