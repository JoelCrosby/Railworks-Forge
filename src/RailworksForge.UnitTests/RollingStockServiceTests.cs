using RailworksForge.Core;
using RailworksForge.Core.External;

namespace RailworksForge.UnitTests;

public class RollingStockServiceTests
{
    [Fact]
    public async Task GetAvailableStock_IgnoresMalformedTrackRulesBeforeConversion()
    {
        var directory = Directory.CreateTempSubdirectory("railworks-stock-");

        try
        {
            var inputPath = Path.Join(directory.FullName, "TrackRules.bin");
            var assembly = typeof(RollingStockServiceTests).Assembly;
            using var resource = assembly.GetManifestResourceStream("RailworksForge.UnitTests.Resources.SerzDuplicateClose.bin")!;
            using (var file = File.Create(inputPath))
            {
                resource.CopyTo(file);
            }

            Assert.Throws<InvalidDataException>(() => SerzInternal.Convert(inputPath));
            var product = new ProductDirectory
            {
                Name = "Test product",
                Path = directory.FullName,
                ContainsRailVehicles = false,
                ContainsPreloadData = false,
            };
            var results = await new RollingStockService().GetAvailableStock(product, CancellationToken.None);

            Assert.Empty(results);
            Assert.Single(Directory.GetFiles(directory.FullName));
        }
        finally
        {
            directory.Delete(true);
        }
    }
}
