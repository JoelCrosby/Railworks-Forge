using System.Diagnostics;

using AngleSharp.Dom;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.External;

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
        if (Paths.Exists(BlueprintPath))
        {
            var converted = await Serz.Convert(BlueprintPath, force: force);
            var text = await File.ReadAllTextAsync(converted.OutputPath);

            return await XmlParser.ParseDocumentAsync(text);
        }

        var archives = Directory.EnumerateFiles(ProductDirectory, "*.ap", SearchOption.AllDirectories);

        foreach (var archive in archives)
        {
            var extracted = Archives.ExtractFileContentFromPath(archive, RelativeBinaryPath, BlueprintPath);

            if (extracted)
            {
                var result = await Serz.Convert(BlueprintPath);
                var text = await File.ReadAllTextAsync(result.OutputPath);

                return await XmlParser.ParseDocumentAsync(text);
            }
        }

        throw new Exception($"unable to get blueprint xml for path {BlueprintPath}");
    }

    public IDocument GetBlueprintXmlInternal()
    {
        if (Paths.Exists(BlueprintPath))
        {
            var data = File.ReadAllBytes(BlueprintPath);
            return new SerzInternal(ref data).ToXml();
        }

        var archives = Directory.EnumerateFiles(ProductDirectory, "*.ap", SearchOption.AllDirectories);

        foreach (var archive in archives)
        {
            var extracted = Archives.ExtractFileContentFromPath(archive, RelativeBinaryPath, BlueprintPath);

            if (extracted)
            {
                var data = File.ReadAllBytes(BlueprintPath);
                return new SerzInternal(ref data).ToXml();
            }
        }

        throw new Exception($"unable to get blueprint xml for path {BlueprintPath}");
    }

    private string ProviderDirectory => Path.Join(
        Paths.GetAssetsDirectory(),
        BlueprintSetIdProvider
    );

    private string ProductDirectory => Path.Join(
        Paths.GetAssetsDirectory(),
        BlueprintSetIdProvider,
        BlueprintSetIdProduct
    );

    private string AgnosticBlueprintIdPath => BlueprintId.Replace('\\', Path.DirectorySeparatorChar);

    public string RelativeBinaryPath => AgnosticBlueprintIdPath.Replace(".xml", ".bin");

    public string BinaryPath => Path.Join(ProductPath, BlueprintIdPath);

    public string ProductPath => Path.Join(Paths.GetAssetsDirectory(), BlueprintSetIdProvider, BlueprintSetIdProduct);

    public string BlueprintIdPath => BlueprintId.Replace('\\', '/').Replace(".xml", ".bin");

    private string BlueprintPath => Path.Join(ProductDirectory, RelativeBinaryPath);
    private string XmlDocumentPath => Path.Join(ProductDirectory, AgnosticBlueprintIdPath);

    public AcquisitionState CachedAcquisitionState { get; private set; }

    private AcquisitionState GetAcquisitionState()
    {
        if (CachedAcquisitionState is AcquisitionState.Found)
        {
            return AcquisitionState.Found;
        }

        CachedAcquisitionState = LoadAcquisitionState();

        return CachedAcquisitionState;
    }

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

        if (Paths.Exists(BlueprintPath, Paths.GetAssetsDirectory()))
        {
            return AcquisitionState.Found;
        }

        if (Paths.Exists(XmlDocumentPath, Paths.GetAssetsDirectory()))
        {
            return AcquisitionState.Found;
        }

        if (Directory.Exists(ProductDirectory))
        {
            var archives = Directory.EnumerateFiles(ProductDirectory, "*.ap", SearchOption.AllDirectories);

            if (archives.Select(archive => Archives.EntryExists(archive, RelativeBinaryPath)).Any(found => found))
            {
                return AcquisitionState.Found;
            }
        }

        if (Directory.Exists(ProviderDirectory))
        {
            var archives = Directory.EnumerateFiles(ProviderDirectory, "*.ap", SearchOption.AllDirectories);

            if (archives.Select(archive => Archives.EntryExists(archive, RelativeBinaryPath)).Any(found => found))
            {
                return AcquisitionState.Found;
            }
        }

        return AcquisitionState.Missing;
    }

    protected static Blueprint Parse(IElement el)
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

        var parts = relative.Split('/', StringSplitOptions.RemoveEmptyEntries);

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
