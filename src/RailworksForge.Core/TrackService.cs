using AngleSharp.Dom;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.External;
using RailworksForge.Core.Models;
using RailworksForge.Core.Models.Common;

namespace RailworksForge.Core;

public class TrackService
{
    public async Task ReplaceTracks(Route route, ReplaceTracksRequest request)
    {
        var document = await route.GetTrackDocument();
        var propertiesDocument = await route.GetRoutePropertiesDocument();

        if (document is null)
        {
            throw new Exception("failed to read route tracks document");
        }

        if (propertiesDocument is null)
        {
            throw new Exception("failed to read route properties document");
        }

        var updatedDocument = UpdateTracks(document, request);
        var updatedPropertiesDocument = UpdateProperties(propertiesDocument, request);

        route.CreateBackup();

        await WriteTracksDocument(updatedDocument, route);
        await WriteRoutePropertiesDocument(updatedPropertiesDocument, route);
    }

    private static IDocument UpdateProperties(IDocument document, ReplaceTracksRequest request)
    {
        foreach (var replacement in request.GetSelectedReplacements())
        {
            if (replacement.ReplacementBlueprint is null)
            {
                continue;
            }

            document.UpdateBlueprintSetCollection(replacement.ReplacementBlueprint, "RBlueprintSetPreLoad");
            document.UpdateBlueprintSetCollection(replacement.ReplacementBlueprint, "RequiredSet");
        }

        return document;
    }

    private static IDocument UpdateTracks(IDocument document, ReplaceTracksRequest request)
    {
        var blueprints = document.QuerySelectorAll("Network-cSectionGenericProperties BlueprintID").ToList();

        var replacementMap = request.Replacements
            .Where(r => r.ReplacementBlueprint is not null)
            .ToDictionary(k => k.Blueprint, v => v.ReplacementBlueprint);

        foreach(var element in blueprints)
        {
            var provider = element.SelectTextContent("Provider");
            var product = element.SelectTextContent("Product");
            var blueprintId = element.SelectTextContent("BlueprintID");

            var blueprint = new Blueprint
            {
                BlueprintId = blueprintId,
                BlueprintSetIdProduct = product,
                BlueprintSetIdProvider = provider,
            };

            if (replacementMap.TryGetValue(blueprint, out var replacement) is false)
            {
                continue;
            }

            if (replacement is null)
            {
                continue;
            }

            element.UpdateTextElement("Provider", replacement.BlueprintSetIdProvider);
            element.UpdateTextElement("Product", replacement.BlueprintSetIdProduct);
            element.UpdateTextElement("BlueprintID", replacement.BlueprintId);
        }

        return document;
    }

    private static async Task WriteTracksDocument(IDocument document, Route route)
    {
        Directory.CreateDirectory(Paths.GetCacheFolder());

        var destination = Path.Join(Paths.GetCacheFolder(), "Tracks.bin.xml");

        await document.ToXmlAsync(destination);

        var output = await Serz.Convert(destination, force: true);
        var destinationDirectory = Path.Join(route.DirectoryPath, "Networks");

        Directory.CreateDirectory(destinationDirectory);

        var binaryDestination = Path.Join(destinationDirectory, "Tracks.bin");

        File.Copy(output.OutputPath, binaryDestination, true);
    }

    private static async Task WriteRoutePropertiesDocument(IDocument document, Route route)
    {
        Directory.CreateDirectory(Paths.GetCacheFolder());

        var destination = Path.Join(Paths.GetCacheFolder(), "RouteProperties.xml");

        await document.ToXmlAsync(destination);

        var documentDestination = Path.Join(route.DirectoryPath, "RouteProperties.xml");

        File.Copy(destination, documentDestination, true);
    }

    public List<DirectoryInfo> GetProviders()
    {
        return Paths.GetAssetProviders();
    }

    public List<DirectoryInfo> GetProducts(string provider)
    {
        return Paths.GetAssetProviderProducts(provider);
    }

    public async Task<List<Track>> GetTracks(
        string providerName,
        DirectoryInfo product,
        CancellationToken cancellationToken)
    {
        var looseBlueprints = GetLooseTrackBlueprints(providerName, product);
        var archivedBlueprints = GetArchivedTrackBlueprints(providerName, product);
        var blueprints = looseBlueprints.Concat(archivedBlueprints).ToList();

        var tracks = new List<Track>();

        foreach (var blueprint in blueprints)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var document = await blueprint.GetXmlDocument();
            var displayName = document.SelectLocalisedStringContent("cTrackSectionBlueprint DisplayName");
            var name = document.SelectTextContent("Name");

            var track = new Track
            {
                Blueprint = blueprint,
                Name = string.IsNullOrEmpty(displayName) ? name : displayName,
            };

            tracks.Add(track);
        }

        return tracks.OrderBy(track => track.Name).ToList();
    }

    private static HashSet<Blueprint> GetLooseTrackBlueprints(string providerName, DirectoryInfo product)
    {
        var networkBinaries = GetTrackBinaryPaths(Path.Join(product.FullName, "RailNetwork"));
        var trackBinaries = GetTrackBinaryPaths(Path.Join(product.FullName, "Track"));

        return networkBinaries
            .Concat(trackBinaries)
            .Select(path =>
            {
                var blueprintId = path
                    .Replace(product.FullName, string.Empty)
                    .TrimStart('/')
                    .Replace('/', '\\')
                    .Replace(".bin", ".xml");

                return new Blueprint
                {
                    BlueprintId = blueprintId,
                    BlueprintSetIdProduct = product.Name,
                    BlueprintSetIdProvider = providerName,
                };
            })
            .ToHashSet();
    }

    private static List<Blueprint> GetArchivedTrackBlueprints(string providerName, DirectoryInfo product)
    {
        var archives = Directory.EnumerateFiles(product.FullName, "*.ap", SearchOption.TopDirectoryOnly);
        var blueprints = new List<Blueprint>();

        foreach (var archive in archives)
        {
            var networkFiles = Archives.ListFilesInPath(archive, "RailNetwork", ".bin");
            var trackFiles = Archives.ListFilesInPath(archive, "Track", ".bin");

            var archiveBlueprints = networkFiles.Concat(trackFiles).Select(file => new Blueprint
            {
                BlueprintId = file.Replace(".XSec", ".xml"),
                BlueprintSetIdProduct = product.Name,
                BlueprintSetIdProvider = providerName,
            });

            blueprints.AddRange(archiveBlueprints);
        }

        return blueprints;
    }

    private static List<string> GetTrackBinaryPaths(string path)
    {
        if (Paths.Exists(path) is false)
        {
            return [];
        }

        var xsecs = Directory.EnumerateFiles(path, "*.XSec", SearchOption.AllDirectories);

        var directories = xsecs
            .Select(Path.GetDirectoryName)
            .Where(x => string.IsNullOrEmpty(x) is false)!
            .ToHashSet<string>();

        return directories
            .SelectMany(d => Directory.EnumerateFiles(d, "*.bin", SearchOption.TopDirectoryOnly))
            .ToList();
    }
}
