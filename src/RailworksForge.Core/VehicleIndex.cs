using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

using RailworksForge.Core.External;
using RailworksForge.Core.Models;
using RailworksForge.Core.Models.Common;

using Serilog;

namespace RailworksForge.Core;

public enum VehicleIndexStage
{
    Discovering,
    Scanning,
    Saving,
    Completed,
}

public sealed record VehicleIndexProgress
{
    public int Sources { get; init; }

    public int TotalSources { get; init; }

    public int Vehicles { get; init; }

    public int Skipped { get; init; }

    public VehicleIndexStage Stage { get; init; }
}

public sealed record VehicleSearchResult(List<RollingStockEntry> Vehicles, int Total);

public sealed class VehicleIndex
{
    private readonly string _assetsPath;
    private readonly VehicleIndexDatabase _database;
    private readonly SemaphoreSlim _refreshLock = new(1);

    public VehicleIndex(string assetsPath, string databaseDirectory)
    {
        _assetsPath = Path.GetFullPath(assetsPath);

        var identity = SHA256.HashData(Encoding.UTF8.GetBytes(_assetsPath));
        var databasePath = Path.Join(databaseDirectory, $"vehicles-v1-{Convert.ToHexString(identity)[..16]}.sqlite");

        _database = new VehicleIndexDatabase(databasePath);
    }

    public Task<VehicleSearchResult> SearchAsync(string? search, int limit, CancellationToken token = default)
    {
        return Task.Run(() => _database.Search(search, limit, token), token);
    }

    public async Task<VehicleIndexProgress> RefreshAsync(
        IProgress<VehicleIndexProgress>? progress = null,
        CancellationToken token = default)
    {
        await _refreshLock.WaitAsync(token);

        try
        {
            var result = await Task.Run(() => Refresh(progress, token), token);

            return result;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private VehicleIndexProgress Refresh(IProgress<VehicleIndexProgress>? progress, CancellationToken token)
    {
        progress?.Report(new VehicleIndexProgress { Stage = VehicleIndexStage.Discovering });

        var paths = FindSources(token);

        using var refresh = _database.BeginRefresh();

        var sourceCount = 0;
        var vehicleCount = 0;
        var skipped = 0;
        var lastProgress = Environment.TickCount64;

        progress?.Report(CreateProgress(VehicleIndexStage.Scanning));

        foreach (var path in paths)
        {
            token.ThrowIfCancellationRequested();

            var archive = Path.GetExtension(path).Equals(".ap", StringComparison.OrdinalIgnoreCase);
            var parts = Path.GetRelativePath(_assetsPath, path).Split(Path.DirectorySeparatorChar);
            var info = new FileInfo(path);

            if (!info.Exists)
            {
                skipped++;
                sourceCount++;
                ReportProgress();
                Log.Warning("Unable to read vehicle source metadata {Path}", path);
                continue;
            }

            var stamp = $"{info.Length}:{info.LastWriteTimeUtc.Ticks}";
            var unchanged = refresh.GetSourceStamp(path) == stamp;

            var skippedBefore = skipped;

            if (!unchanged)
            {
                refresh.DeleteSourceVehicles(path);

                try
                {
                    if (archive)
                    {
                        using var package = ZipFile.OpenRead(path);

                        foreach (var entry in package.Entries)
                        {
                            token.ThrowIfCancellationRequested();

                            var entryPath = entry.FullName.Replace('\\', '/');
                            var validPath = !entryPath.StartsWith('/') && !entryPath.Split('/').Contains("..");

                            if (!validPath || !entryPath.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            using var stream = entry.Open();

                            var archiveDirectory = string.Join('/', parts.Skip(2).SkipLast(1));
                            var relativePath = string.IsNullOrEmpty(archiveDirectory)
                                ? entryPath
                                : archiveDirectory + "/" + entryPath;

                            ReadVehicle(stream, relativePath, 0);
                            ReportProgress();
                        }
                    }
                    else
                    {
                        using var stream = File.OpenRead(path);
                        ReadVehicle(stream, string.Join('/', parts.Skip(2)), 1);
                    }
                }
                catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException)
                {
                    skipped++;
                    Log.Warning(exception, "Unable to index vehicle source {Path}", path);
                }
            }

            var savedStamp = skipped == skippedBefore ? stamp : "";
            refresh.SaveSource(path, savedStamp);
            sourceCount++;

            if (sourceCount % 100 == 0)
            {
                progress?.Report(CreateProgress(VehicleIndexStage.Scanning));
            }

            ReportProgress();

            void ReadVehicle(Stream stream, string relativePath, int priority)
            {
                try
                {
                    using var buffer = new MemoryStream();

                    stream.CopyTo(buffer);

                    var bytes = buffer.ToArray();
                    var reader = new SerzReader(bytes);

                    var vehicleHeader = reader.Read() && reader.Kind == SerzNodeKind.Open
                        && reader.Name == "cBlueprintLoader" && reader.Read() && reader.Kind == SerzNodeKind.Open
                        && reader.Name == "Blueprint" && reader.Read() && reader.Kind == SerzNodeKind.Open;

                    if (!vehicleHeader)
                    {
                        return;
                    }

                    var type = Utilities.ParseBlueprintType(reader.Name);
                    var isVehicle = type is BlueprintType.Engine or BlueprintType.Wagon or BlueprintType.Tender;

                    if (!isVehicle)
                    {
                        return;
                    }

                    using var document = new SerzInternal(ref bytes).ToXml();
                    var element = document.QuerySelector("cBlueprintLoader > Blueprint");

                    if (element is null)
                    {
                        return;
                    }

                    var blueprint = new Blueprint
                    {
                        BlueprintSetIdProvider = parts[0],
                        BlueprintSetIdProduct = parts[1],
                        BlueprintId = Path.ChangeExtension(relativePath, ".xml").Replace('/', '\\'),
                    };

                    var parsed = RollingStockEntry.Parse(element, blueprint);
                    var hasDisplayName = !string.IsNullOrWhiteSpace(parsed.DisplayName);
                    var vehicle = new RollingStockEntry
                    {
                        Blueprint = blueprint,
                        DisplayName = hasDisplayName ? parsed.DisplayName : parsed.LocomotiveName,
                        LocomotiveName = parsed.LocomotiveName,
                        BlueprintType = type,
                    };

                    var identity = $"{parts[0]}/{parts[1]}/{relativePath}".ToUpperInvariant();

                    refresh.AddVehicle(path, identity, priority, vehicle);
                    vehicleCount++;
                }
                catch (Exception exception) when (exception is IOException or InvalidDataException or ArgumentException)
                {
                    skipped++;
                    Log.Warning(exception, "Unable to index vehicle {Source}/{Entry}", path, relativePath);
                }
            }
        }

        void ReportProgress()
        {
            var now = Environment.TickCount64;

            if (now - lastProgress < 250)
            {
                return;
            }

            lastProgress = now;
            progress?.Report(CreateProgress(VehicleIndexStage.Scanning));
        }

        VehicleIndexProgress CreateProgress(VehicleIndexStage stage)
        {
            return new VehicleIndexProgress
            {
                Sources = sourceCount,
                TotalSources = paths.Count,
                Vehicles = vehicleCount,
                Skipped = skipped,
                Stage = stage,
            };
        }

        progress?.Report(CreateProgress(VehicleIndexStage.Saving));

        vehicleCount = refresh.Commit(token);

        var result = CreateProgress(VehicleIndexStage.Completed);

        progress?.Report(result);

        return result;
    }

    private List<string> FindSources(CancellationToken token)
    {
        var paths = new List<string>();
        var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = false };

        foreach (var path in Directory.EnumerateFiles(_assetsPath, "*", options))
        {
            token.ThrowIfCancellationRequested();

            var extension = Path.GetExtension(path);
            var isSource = extension.Equals(".ap", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".bin", StringComparison.OrdinalIgnoreCase);
            var parts = Path.GetRelativePath(_assetsPath, path).Split(Path.DirectorySeparatorChar);

            if (isSource && parts.Length >= 3)
            {
                paths.Add(path);
            }
        }

        token.ThrowIfCancellationRequested();

        return paths;
    }
}
