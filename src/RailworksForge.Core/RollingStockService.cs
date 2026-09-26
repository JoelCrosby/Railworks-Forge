using System.Collections.Concurrent;

using RailworksForge.Core.External;
using RailworksForge.Core.Models;
using RailworksForge.Core.Models.Common;

namespace RailworksForge.Core;

public class RollingStockService
{
    private const int MaxParallelConversions = 4;

    public async Task<List<RollingStockEntry>> GetAvailableStock(
        ProductDirectory directory,
        CancellationToken cancellationToken)
    {
        var binFiles = GetStockBinFiles(directory, cancellationToken);
        var results = new ConcurrentBag<RollingStockEntry>();
        var options = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = MaxParallelConversions,
        };

        await Parallel.ForEachAsync(binFiles, options, async (binFile, token) =>
        {
            var exported = await Serz.Convert(binFile, token);
            var entries = await ReadStockEntries(exported.OutputPath, token);

            foreach (var entry in entries)
            {
                results.Add(entry);
            }
        });

        return results.OrderBy(entry => entry.DisplayName).ToList();
    }

    private static List<string> GetStockBinFiles(ProductDirectory directory, CancellationToken cancellationToken)
    {
        var binFiles = Directory
            .EnumerateFiles(directory.Path, "*.bin", SearchOption.AllDirectories)
            .Where(path => !Path.GetFileName(path).Equals("MetaData.bin", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var package in Directory.EnumerateFiles(directory.Path, "*.ap", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            binFiles.AddRange(Archives.ExtractFilesOfType(package, ".bin"));
        }

        return binFiles;
    }

    private static async Task<List<RollingStockEntry>> ReadStockEntries(string path, CancellationToken cancellationToken)
    {
        await using var text = File.OpenRead(path);
        using var document = await XmlParser.ParseDocumentAsync(text, cancellationToken);
        var blueprint = Blueprint.FromPath(path);

        return document
            .QuerySelectorAll("Blueprint")
            .Select(el => RollingStockEntry.Parse(el, blueprint))
            .Where(entry => entry.BlueprintType is BlueprintType.Engine or BlueprintType.Tender or BlueprintType.Wagon)
            .ToList();
    }
}
