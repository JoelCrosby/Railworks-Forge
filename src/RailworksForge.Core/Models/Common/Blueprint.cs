using System.Diagnostics;

using AngleSharp.Dom;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.External;

using Serilog;

namespace RailworksForge.Core.Models.Common;

[DebuggerDisplay("{BlueprintSetIdProvider}/{BlueprintSetIdProduct}/{BlueprintId}")]
public class Blueprint
{
    public required string BlueprintId { get; init; }

    public required string BlueprintSetIdProvider { get; init; }

    public required string BlueprintSetIdProduct { get; init; }

    public override bool Equals(object? other) => this.Equals(other as Blueprint);

    private bool Equals(Blueprint? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (this.GetType() != other.GetType())
        {
            return false;
        }

        return BlueprintId == other.BlueprintId &&
               BlueprintSetIdProvider == other.BlueprintSetIdProvider &&
               BlueprintSetIdProduct == other.BlueprintSetIdProduct;
    }

    public override int GetHashCode()
    {
        return (BlueprintId, BlueprintSetIdProvider, BlueprintSetIdProduct).GetHashCode();
    }

    public AcquisitionState AcquisitionState => GetAcquisitionState();

    private static readonly Dictionary<string, Blueprint> BlueprintCache = new ();

    public async Task<IDocument> GetXmlDocument(bool force = false)
    {
        if (Paths.Exists(BlueprintBinaryPath))
        {
            var converted = await Serz.Convert(BlueprintBinaryPath, force: force);
            var file = File.OpenRead(converted.OutputPath);

            return await XmlParser.ParseDocumentAsync(file);
        }

        var archives = Directory.EnumerateFiles(ProductDirectory, "*.ap", SearchOption.AllDirectories);

        foreach (var archive in archives)
        {
            var destination = Path.Join(Paths.CacheOutputPath, RelativeBinaryPath);
            var extracted = Archives.ExtractFileContentFromPath(archive, RelativeBinaryPath, destination);

            if (extracted)
            {
                var result = await Serz.Convert(destination);
                var file = File.OpenRead(result.OutputPath);

                return await XmlParser.ParseDocumentAsync(file);
            }
        }

        throw new Exception($"unable to get blueprint xml for path {BlueprintBinaryPath}");
    }

    public IDocument GetBlueprintXmlInternal()
    {
        if (Paths.Exists(BlueprintBinaryPath))
        {
            var data = File.ReadAllBytes(BlueprintBinaryPath);
            return new SerzInternal(ref data).ToXml();
        }

        var archives = Directory.EnumerateFiles(ProductDirectory, "*.ap", SearchOption.AllDirectories);

        foreach (var archive in archives)
        {
            var extracted = Archives.ExtractFileContentFromPath(archive, RelativeBinaryPath, BlueprintBinaryPath);

            if (extracted)
            {
                var data = File.ReadAllBytes(BlueprintBinaryPath);
                return new SerzInternal(ref data).ToXml();
            }
        }

        throw new Exception($"unable to get blueprint xml for path {BlueprintBinaryPath}");
    }

    private string ProductDirectory => Path.Join(
        Paths.GetAssetsDirectory(),
        BlueprintSetIdProvider,
        BlueprintSetIdProduct
    );

    private string AgnosticBlueprintIdPath => BlueprintId.Replace('\\', Path.DirectorySeparatorChar);

    public string RelativeBinaryPath => AgnosticBlueprintIdPath.Replace(".xml", ".bin");

    public string BinaryPath => Path.Join(ProductPath, BlueprintIdPath);

    public string? BinaryDirectoryPath => Path.GetDirectoryName(BinaryPath);

    public string ProductPath => Path.Join(Paths.GetAssetsDirectory(), BlueprintSetIdProvider, BlueprintSetIdProduct);

    public string BlueprintIdPath => BlueprintId.Replace('\\', '/').Replace(".xml", ".bin");

    private string BlueprintBinaryPath => Path.Join(ProductDirectory, RelativeBinaryPath);

    private string BlueprintXmlPath => Path.Join(ProductDirectory, AgnosticBlueprintIdPath);

    private AcquisitionState GetAcquisitionState()
    {
        if (Cache.BlueprintAcquisitionStates.GetValueOrDefault(RelativeBinaryPath) is {} cached)
        {
            return cached;
        }

        var state = LoadAcquisitionState();

        Cache.BlueprintAcquisitionStates.TryAdd(RelativeBinaryPath, state);

        return state;
    }

    private static readonly Lock ArchiveLock = new ();

    private AcquisitionState LoadAcquisitionState()
    {
        if (string.IsNullOrEmpty(BlueprintSetIdProvider))
        {
            return AcquisitionState.Missing;
        }

        if (string.IsNullOrEmpty(BlueprintSetIdProduct))
        {
            return AcquisitionState.Missing;
        }

        if (Paths.Exists(BlueprintBinaryPath, Paths.GetAssetsDirectory()))
        {
            return AcquisitionState.Found;
        }

        if (Paths.Exists(BlueprintXmlPath, Paths.GetAssetsDirectory()))
        {
            return AcquisitionState.Found;
        }

        lock (ArchiveLock)
        {
            if (Paths.GetActualPathFromInsensitive(ProductDirectory) is {} actualProductDirectory)
            {
                var archives = Directory.EnumerateFiles(actualProductDirectory, "*.ap", SearchOption.AllDirectories).ToList();

                Log.Debug("searching for blueprint {Blueprint} in archives {Archives}", RelativeBinaryPath, archives);

                if (archives.Any(archive => Archives.EntryExists(archive, RelativeBinaryPath)))
                {
                    return AcquisitionState.Found;
                }
            }
        }

        return AcquisitionState.Missing;
    }

    public static Blueprint Parse(IElement el)
    {
        var blueprintId = el.SelectTextContent("BlueprintID");

        if (BlueprintCache.GetValueOrDefault(blueprintId) is {} cached)
        {
            return cached;
        }

        var blueprintSetIdProduct = el.SelectTextContent("iBlueprintLibrary-cBlueprintSetID Product");
        var blueprintSetIdProvider = el.SelectTextContent("iBlueprintLibrary-cBlueprintSetID Provider");

        var result = new Blueprint
        {
            BlueprintId = blueprintId,
            BlueprintSetIdProduct = blueprintSetIdProduct,
            BlueprintSetIdProvider = blueprintSetIdProvider,
        };

        BlueprintCache.Add(blueprintId, result);

        return result;
    }

    public static Blueprint FromPath(string path)
    {
        var relative = path
            .Replace(Paths.GetAssetsDirectory(), string.Empty)
            .Replace(Paths.CacheOutputPath, string.Empty);

        var parts = relative
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !p.Contains(Paths.ArchivePathPreserveSuffix))
            .ToArray();

        if (parts.Length < 3)
        {
            throw new Exception($"unable to parse blueprint from path {path}");
        }

        var provider = parts[1];
        var product = parts[2];

        var assetPath = string.Join("\\", parts.Skip(3)).Replace(".bin.xml", ".xml");

        return new Blueprint
        {
            BlueprintSetIdProduct = product,
            BlueprintSetIdProvider = provider,
            BlueprintId = assetPath,
        };
    }
}
