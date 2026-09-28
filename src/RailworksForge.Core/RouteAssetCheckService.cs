using System.Collections.Concurrent;

using Dameng.SepEx;

using nietras.SeparatedValues;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.External;
using RailworksForge.Core.Models;
using RailworksForge.Core.Models.Common;

namespace RailworksForge.Core;

public enum AssetCheckStage
{
    ReadingFiles,
    CheckingBlueprints,
}

public record AssetCheckProgress
{
    public required AssetCheckStage Stage { get; init; }

    public required int Percentage { get; init; }

    public required int Completed { get; init; }

    public required int Total { get; init; }
}

public class RouteAssetCheckService
{
    private const int MaxParallelConversions = 4;

    public async Task<List<Blueprint>> FindMissingAssets(
        Route route,
        IProgress<AssetCheckProgress> progress,
        CancellationToken cancellationToken)
    {
        var blueprints = await GetRouteBlueprints(route, progress, cancellationToken);
        var missing = GetMissingAssets(blueprints, progress, cancellationToken);

        return missing;
    }

    private static async Task<List<Blueprint>> GetRouteBlueprints(
        Route route,
        IProgress<AssetCheckProgress> progress,
        CancellationToken cancellationToken)
    {
        var cachePath = Paths.GetRouteAssetsCachePath(route);
        var looseSceneryFiles = GetLooseBinFiles(route, "Scenery", true);
        var looseNetworkFiles = GetLooseBinFiles(route, "Networks", false);
        var looseFiles = looseSceneryFiles.Concat(looseNetworkFiles).ToList();

        if (IsCacheCurrent(cachePath, route, looseFiles))
        {
            return ReadCachedBlueprints(cachePath);
        }

        var archivedFiles = GetArchivedBinFiles(route, looseFiles);
        var binFiles = looseFiles.Concat(archivedFiles).ToList();

        var blueprints = await ReadBlueprintsFromBinaries(binFiles, progress, cancellationToken);

        WriteCachedBlueprints(cachePath, blueprints);

        return blueprints;
    }

    private static bool IsCacheCurrent(string cachePath, Route route, List<string> looseFiles)
    {
        if (!File.Exists(cachePath))
        {
            return false;
        }

        var cachedAt = File.GetLastWriteTimeUtc(cachePath);
        var sourcePaths = looseFiles.Append(route.MainContentArchivePath).Where(File.Exists);
        var hasNewerSource = sourcePaths.Any(path => File.GetLastWriteTimeUtc(path) > cachedAt);

        return !hasNewerSource;
    }

    private static List<Blueprint> ReadCachedBlueprints(string path)
    {
        using var reader = Sep.Reader().FromFile(path);

        return reader.GetRecords<Blueprint>().ToList();
    }

    private static void WriteCachedBlueprints(string path, IEnumerable<Blueprint> blueprints)
    {
        var directory = Directory.GetParent(path)?.FullName;

        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        var stagingPath = $"{path}.{Guid.NewGuid():N}.tmp";

        try
        {
            using (var writer = Sep.Writer().ToFile(stagingPath))
            {
                writer.WriteRecords(blueprints);
            }

            File.Move(stagingPath, path, overwrite: true);
        }
        finally
        {
            File.Delete(stagingPath);
        }
    }

    private static async Task<List<Blueprint>> ReadBlueprintsFromBinaries(
        List<string> binFiles,
        IProgress<AssetCheckProgress> progress,
        CancellationToken cancellationToken)
    {
        var results = new ConcurrentDictionary<Blueprint, byte>();
        var processedCount = 0;
        var lastPercentage = -1;
        var options = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = MaxParallelConversions,
        };

        await Parallel.ForEachAsync(binFiles, options, async (path, token) =>
        {
            try
            {
                foreach (var blueprint in await ReadBinaryBlueprints(path, token))
                {
                    results.TryAdd(blueprint, byte.MinValue);
                }

                var count = Interlocked.Increment(ref processedCount);
                var percentage = (int)(100L * count / binFiles.Count);
                var isNewPercentage = Interlocked.Exchange(ref lastPercentage, percentage) != percentage;

                if (isNewPercentage)
                {
                    var fileProgress = new AssetCheckProgress
                    {
                        Stage = AssetCheckStage.ReadingFiles,
                        Percentage = percentage,
                        Completed = count,
                        Total = binFiles.Count,
                    };

                    Report(progress, fileProgress, token);
                }
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                throw new IOException($"Unable to check {path}", e);
            }
        });

        return results.Keys.ToList();
    }

    private static async Task<List<Blueprint>> ReadBinaryBlueprints(string path, CancellationToken cancellationToken)
    {
        var serialised = await Serz.Convert(path, cancellationToken);
        await using var xml = File.OpenRead(serialised.OutputPath);

        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
        using var document = XmlParser.ParseDocument(xml);

        // Scenery tiles reference blueprints from dynamic entities; track, road and loft networks from section properties.
        const string blueprintSelector =
            "cDynamicEntity iBlueprintLibrary-cAbsoluteBlueprintID, Network-cSectionGenericProperties iBlueprintLibrary-cAbsoluteBlueprintID";

        return document
            .QuerySelectorAll(blueprintSelector)
            .Select(el => new Blueprint
            {
                BlueprintSetIdProvider = el.SelectTextContent("Provider"),
                BlueprintSetIdProduct = el.SelectTextContent("Product"),
                BlueprintId = el.SelectTextContent("BlueprintID"),
            })
            .Where(blueprint => !string.IsNullOrWhiteSpace(blueprint.BlueprintId))
            .ToList();
    }

    private static List<Blueprint> GetMissingAssets(
        List<Blueprint> blueprints,
        IProgress<AssetCheckProgress> progress,
        CancellationToken cancellationToken)
    {
        var notFound = new List<Blueprint>();
        var lastPercentage = -1;

        for (var index = 0; index < blueprints.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var blueprint = blueprints[index];

            if (blueprint.AcquisitionState is not AcquisitionState.Found)
            {
                notFound.Add(blueprint);
            }

            var count = index + 1;
            var percentage = (int)(100L * count / blueprints.Count);

            if (percentage != lastPercentage)
            {
                lastPercentage = percentage;
                var blueprintProgress = new AssetCheckProgress
                {
                    Stage = AssetCheckStage.CheckingBlueprints,
                    Percentage = percentage,
                    Completed = count,
                    Total = blueprints.Count,
                };

                Report(progress, blueprintProgress, cancellationToken);
            }
        }

        return notFound.OrderBy(blueprint => blueprint.BlueprintSetIdProvider).ToList();
    }

    private static List<string> GetLooseBinFiles(Route route, string directory, bool allDirectories)
    {
        var absolutePath = Paths.GetActualPathFromInsensitive(Path.Join(route.DirectoryPath, directory));
        var searchOption = allDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        if (absolutePath is null)
        {
            return [];
        }

        return Directory.EnumerateFiles(absolutePath, "*.bin", searchOption).ToList();
    }

    // Loose files take precedence in game, so only archived files without a loose copy are extracted.
    private static List<string> GetArchivedBinFiles(Route route, List<string> looseFiles)
    {
        var archivePath = route.MainContentArchivePath;

        if (!File.Exists(archivePath))
        {
            return [];
        }

        var looseRelativePaths = looseFiles
            .Select(path => Path.GetRelativePath(route.DirectoryPath, path).Replace('\\', '/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Archives.ExtractEntriesToCache(archivePath, entryPath =>
        {
            var normalisedPath = entryPath.Replace('\\', '/');
            var isBinary = normalisedPath.EndsWith(".bin", StringComparison.OrdinalIgnoreCase);
            var isScenery = normalisedPath.StartsWith("Scenery/", StringComparison.OrdinalIgnoreCase);
            var isNetwork = normalisedPath.StartsWith("Networks/", StringComparison.OrdinalIgnoreCase)
                && normalisedPath.Count(c => c == '/') is 1;
            var hasLooseCopy = looseRelativePaths.Contains(normalisedPath);

            return isBinary && (isScenery || isNetwork) && !hasLooseCopy;
        });
    }

    private static void Report(
        IProgress<AssetCheckProgress> progress,
        AssetCheckProgress update,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        progress.Report(update);
    }
}
