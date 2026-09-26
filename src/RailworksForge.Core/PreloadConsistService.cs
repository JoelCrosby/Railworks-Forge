using AngleSharp.Dom;

using RailworksForge.Core.External;
using RailworksForge.Core.Models;

namespace RailworksForge.Core;

public class PreloadConsistService
{
    public async Task<List<PreloadConsist>> GetPreloadConsists(
        BrowserDirectory directory,
        CancellationToken cancellationToken)
    {
        var preloadDirectory = GetPreloadDirectory(directory);

        if (preloadDirectory is null || !Paths.Exists(preloadDirectory))
        {
            return [];
        }

        var consists = new List<PreloadConsist>();

        foreach (var binFile in Directory.EnumerateFiles(preloadDirectory, "*.bin", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var exported = await Serz.Convert(binFile, cancellationToken);
            var fileConsists = await ReadConsists(exported.OutputPath, cancellationToken);

            consists.AddRange(fileConsists);
        }

        return consists;
    }

    private static string? GetPreloadDirectory(BrowserDirectory directory)
    {
        var assetPath = directory.AssetDirectory.Path;
        var preloadDirectory = FindPreloadDirectory(assetPath);

        if (preloadDirectory is not null && Paths.Exists(preloadDirectory))
        {
            return preloadDirectory;
        }

        foreach (var package in Directory.EnumerateFiles(assetPath, "*.ap"))
        {
            Archives.ExtractDirectory(package, "PreLoad");
        }

        return FindPreloadDirectory(assetPath);
    }

    private static string? FindPreloadDirectory(string assetPath)
    {
        return Directory
            .GetDirectories(assetPath)
            .FirstOrDefault(path => path.Contains("PreLoad", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<List<PreloadConsist>> ReadConsists(string path, CancellationToken cancellationToken)
    {
        await using var file = File.OpenRead(path);
        using var document = await XmlParser.ParseDocumentAsync(file, cancellationToken);

        var blueprints = document.QuerySelectorAll("Blueprint cConsistBlueprint").ToList();
        var consists = new List<PreloadConsist>();

        await CollectConsists(blueprints, consists);

        return consists;
    }

    private static async Task CollectConsists(List<IElement> elements, List<PreloadConsist> consists)
    {
        foreach (var element in elements)
        {
            var parsed = PreloadConsist.Parse(element);

            if (parsed is null)
            {
                continue;
            }

            var isFragment = parsed.Blueprint.BlueprintId.Contains("fragment", StringComparison.OrdinalIgnoreCase);

            if (!isFragment)
            {
                consists.Add(parsed);
                continue;
            }

            using var fragmentDocument = await parsed.Blueprint.GetXmlDocument();
            var fragmentBlueprints = fragmentDocument.QuerySelectorAll("Blueprint cConsistFragmentBlueprint").ToList();

            await CollectConsists(fragmentBlueprints, consists);
        }
    }
}
