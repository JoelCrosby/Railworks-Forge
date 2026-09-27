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

        if (Paths.Exists(cachePath))
        {
            return ReadCachedBlueprints(cachePath);
        }

        var sceneryBinFiles = GetBinFiles(route, "Scenery", true);
        var networkBinFiles = GetBinFiles(route, "Networks", false);
        var binFiles = sceneryBinFiles.Concat(networkBinFiles).ToList();

        var blueprints = await ReadBlueprintsFromBinaries(binFiles, progress, cancellationToken);

        await WriteCachedBlueprints(cachePath, blueprints);

        return blueprints;
    }

    private static List<Blueprint> ReadCachedBlueprints(string path)
    {
        using var reader = Sep.Reader().FromFile(path);

        return reader.GetRecords<Blueprint>().ToList();
    }

    private static async Task WriteCachedBlueprints(string path, IEnumerable<Blueprint> blueprints)
    {
        var directory = Directory.GetParent(path)?.FullName;

        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        await using var writer = Sep.Writer().ToFile(path);

        writer.WriteRecords(blueprints);
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
            catch (Exception e)
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

        return document
            .QuerySelectorAll("cDynamicEntity BlueprintID")
            .Select(el => new Blueprint
            {
                BlueprintSetIdProvider = el.SelectTextContent("iBlueprintLibrary-cAbsoluteBlueprintID Provider"),
                BlueprintSetIdProduct = el.SelectTextContent("iBlueprintLibrary-cAbsoluteBlueprintID Product"),
                BlueprintId = el.SelectTextContent("iBlueprintLibrary-cAbsoluteBlueprintID BlueprintID"),
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

    private static List<string> GetBinFiles(Route route, string directory, bool allDirectories)
    {
        var absolutePath = Path.Join(route.DirectoryPath, directory);
        var searchOption = allDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        if (Paths.Exists(absolutePath))
        {
            return Directory.EnumerateFiles(absolutePath, "*.bin", searchOption).ToList();
        }

        var archivePath = route.MainContentArchivePath;

        if (Paths.Exists(archivePath) is false)
        {
            throw new Exception($"Could not find archive at {archivePath}");
        }

        Archives.ExtractDirectory(archivePath, directory);

        return Directory.EnumerateFiles(absolutePath, "*.bin", searchOption).ToList();
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
