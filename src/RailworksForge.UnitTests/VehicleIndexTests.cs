using System.IO.Compression;
using System.Text;

using RailworksForge.Core;

namespace RailworksForge.UnitTests;

public sealed class VehicleIndexTests : IDisposable
{
    private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("vehicle-index-tests-");

    [Fact]
    public async Task Index_PersistsSearchableVehiclesAndPrefersLooseOverrides()
    {
        var assets = Path.Join(_directory.FullName, "Assets");
        var product = Directory.CreateDirectory(Path.Join(assets, "Provider", "Product"));
        var archivePath = Path.Join(product.FullName, "Vehicles.ap");

        using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
        {
            WriteEntry(archive, "RailVehicles/Engine.bin", Vehicle("Archived engine"));
            WriteEntry(archive, "RailVehicles/Wagon.bin", Vehicle("Goods wagon", "cWagonBlueprint"));
            WriteEntry(archive, "RailVehicles/Bad.bin", [1, 2, 3]);
            WriteEntry(archive, "../Unsafe.bin", Vehicle("Unsafe"));
        }

        var loose = Path.Join(product.FullName, "RailVehicles", "Engine.bin");

        Directory.CreateDirectory(Path.GetDirectoryName(loose)!);
        File.WriteAllBytes(loose, Vehicle("Express locomotive"));
        File.WriteAllBytes(Path.Join(product.FullName, "Scenery.bin"), Vehicle("Scenery", "cSceneryBlueprint"));

        var index = new VehicleIndex(assets, _directory.FullName);
        var progress = await index.RefreshAsync();

        Assert.Equal(2, progress.Vehicles);
        Assert.Equal(1, progress.Skipped);

        var result = await index.SearchAsync("provider exp", 200);
        var vehicle = Assert.Single(result.Vehicles);

        Assert.Equal("Express locomotive", vehicle.DisplayName);
        Assert.Equal(@"RailVehicles\Engine.xml", vehicle.Blueprint.BlueprintId);
        Assert.Equal("Product", vehicle.Blueprint.BlueprintSetIdProduct);
        Assert.Empty((await index.SearchAsync("Archived", 200)).Vehicles);
        Assert.Empty((await index.SearchAsync("\" OR *", 200)).Vehicles);

        var reopened = new VehicleIndex(assets, _directory.FullName);

        Assert.Equal(2, (await reopened.SearchAsync(null, 1)).Total);
        Assert.Single((await reopened.SearchAsync(null, 1)).Vehicles);
        File.Delete(loose);

        await reopened.RefreshAsync();

        Assert.Single((await reopened.SearchAsync("Archived", 200)).Vehicles);
        File.Delete(archivePath);

        await reopened.RefreshAsync();

        Assert.Empty((await reopened.SearchAsync(null, 200)).Vehicles);
    }

    [Fact]
    public async Task Refresh_SkipsUnchangedSourcesAndDetectsChangedVehicles()
    {
        var product = Directory.CreateDirectory(Path.Join(_directory.FullName, "Assets", "Provider", "Product"));
        var path = Path.Join(product.FullName, "Engine.bin");

        File.WriteAllBytes(path, Vehicle("Original"));

        var index = new VehicleIndex(Path.Join(_directory.FullName, "Assets"), _directory.FullName);

        await index.RefreshAsync();

        var originalStamp = File.GetLastWriteTimeUtc(path);

        File.WriteAllBytes(path, Vehicle("Modified"));
        File.SetLastWriteTimeUtc(path, originalStamp);

        await index.RefreshAsync();

        Assert.Single((await index.SearchAsync("Original", 200)).Vehicles);
        File.SetLastWriteTimeUtc(path, originalStamp.AddSeconds(1));

        await index.RefreshAsync();

        Assert.Single((await index.SearchAsync("Modified", 200)).Vehicles);
    }

    [Fact]
    public async Task Refresh_KeepsPreviousSnapshotSearchableAndRollsBackCancellation()
    {
        var assets = Path.Join(_directory.FullName, "Assets");
        var product = Directory.CreateDirectory(Path.Join(assets, "Provider", "Product"));

        File.WriteAllBytes(Path.Join(product.FullName, "Original.bin"), Vehicle("Original"));

        var index = new VehicleIndex(assets, _directory.FullName);

        await index.RefreshAsync();

        for (var number = 0; number < 110; number++)
        {
            File.WriteAllBytes(Path.Join(product.FullName, $"New{number}.bin"), Vehicle("New vehicle"));
        }

        using var cancellation = new CancellationTokenSource();

        var progress = new CallbackProgress(value =>
        {

            if (value.Stage != VehicleIndexStage.Scanning || value.Sources < 100)
            {
                return;
            }

            var results = index.SearchAsync(null, 200).GetAwaiter().GetResult();

            Assert.Single(results.Vehicles);
            cancellation.Cancel();
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => index.RefreshAsync(progress, cancellation.Token));

        Assert.Single((await index.SearchAsync(null, 200)).Vehicles);
    }

    [Fact]
    public async Task Index_HandlesNestedArchivesAndRejectsMalformedVehicles()
    {
        var assets = Path.Join(_directory.FullName, "Assets");
        var directory = Directory.CreateDirectory(Path.Join(assets, "Provider", "Product", "RailVehicles"));

        using (var archive = ZipFile.Open(Path.Join(directory.FullName, "Nested.ap"), ZipArchiveMode.Create))
        {
            WriteEntry(archive, "Tender.BIN", Vehicle("Steam tender", "cTenderBlueprint"));
            WriteEntry(archive, "Broken.bin", Vehicle("Broken engine")[..^1]);
        }

        var index = new VehicleIndex(assets, _directory.FullName);
        var progress = await index.RefreshAsync();
        var result = await index.SearchAsync("steam", 200);
        var vehicle = Assert.Single(result.Vehicles);

        Assert.Equal(BlueprintType.Tender, vehicle.BlueprintType);
        Assert.Equal(@"RailVehicles\Tender.xml", vehicle.Blueprint.BlueprintId);
        Assert.Equal(1, progress.Skipped);
    }

    [Fact]
    public async Task Database_IsScopedToInstallation()
    {
        var first = Directory.CreateDirectory(Path.Join(_directory.FullName, "First", "Provider", "Product"));

        File.WriteAllBytes(Path.Join(first.FullName, "Engine.bin"), Vehicle("First"));

        var firstIndex = new VehicleIndex(Path.Join(_directory.FullName, "First"), _directory.FullName);

        await firstIndex.RefreshAsync();

        var second = Directory.CreateDirectory(Path.Join(_directory.FullName, "Second"));
        var secondIndex = new VehicleIndex(second.FullName, _directory.FullName);

        Assert.Empty((await secondIndex.SearchAsync(null, 200)).Vehicles);
    }

    [Fact]
    public async Task Refresh_ReportsFileTotalsAndCompletionForInitialAndIncrementalScans()
    {
        var assets = Path.Join(_directory.FullName, "Assets");
        var product = Directory.CreateDirectory(Path.Join(assets, "Provider", "Product"));

        File.WriteAllBytes(Path.Join(product.FullName, "Engine.bin"), Vehicle("Engine"));
        File.WriteAllBytes(Path.Join(product.FullName, "Broken.bin"), [1, 2, 3]);
        File.WriteAllText(Path.Join(product.FullName, "Ignored.xml"), "ignored");

        using (var archive = ZipFile.Open(Path.Join(product.FullName, "Stock.ap"), ZipArchiveMode.Create))
        {
            WriteEntry(archive, "Wagon.bin", Vehicle("Wagon", "cWagonBlueprint"));
        }

        var index = new VehicleIndex(assets, _directory.FullName);

        for (var scan = 0; scan < 2; scan++)
        {
            var updates = new List<VehicleIndexProgress>();
            var progress = new CallbackProgress(updates.Add);
            var result = await index.RefreshAsync(progress);

            Assert.Equal(VehicleIndexStage.Discovering, updates[0].Stage);
            Assert.Contains(updates, update => update.Stage == VehicleIndexStage.Saving);
            Assert.Equal(VehicleIndexStage.Completed, updates[^1].Stage);
            Assert.Equal(3, result.TotalSources);
            Assert.Equal(result.TotalSources, result.Sources);
            Assert.Equal(2, result.Vehicles);
            Assert.Equal(1, result.Skipped);
            Assert.All(updates, update => Assert.InRange(update.Sources, 0, update.TotalSources));
        }
    }

    private static void WriteEntry(ZipArchive archive, string path, byte[] bytes)
    {
        using var stream = archive.CreateEntry(path).Open();
        stream.Write(bytes);
    }

    private static byte[] Vehicle(string name, string type = "cEngineBlueprint")
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write("SERZ\0\0\x01\0"u8);

        Open("cBlueprintLoader");
        Open("Blueprint");
        Open(type);
        Value("Name", name);
        Open("DisplayName");
        Open("Localisation-cUserLocalisedString");
        Value("English", name);
        Close("Localisation-cUserLocalisedString");
        Close("DisplayName");
        Close(type);
        Close("Blueprint");
        Close("cBlueprintLoader");

        return stream.ToArray();

        void String(string text)
        {
            writer.Write(ushort.MaxValue);
            writer.Write(text.Length);
            writer.Write(Encoding.UTF8.GetBytes(text));
        }

        void Open(string element)
        {
            writer.Write((byte)255);
            writer.Write((byte)'P');
            String(element);
            writer.Write(0);
            writer.Write(1);
        }

        void Close(string element)
        {
            writer.Write((byte)255);
            writer.Write((byte)'p');
            String(element);
        }

        void Value(string element, string text)
        {
            writer.Write((byte)255);
            writer.Write((byte)'V');
            String(element);
            String("cDeltaString");
            String(text);
        }
    }

    private sealed class CallbackProgress(Action<VehicleIndexProgress> callback) : IProgress<VehicleIndexProgress>
    {
        public void Report(VehicleIndexProgress value)
        {
            callback(value);
        }
    }

    public void Dispose()
    {
        _directory.Delete(true);
    }
}
