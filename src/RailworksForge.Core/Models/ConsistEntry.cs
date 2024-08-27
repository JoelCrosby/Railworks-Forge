using AngleSharp.Dom;

using RailworksForge.Core.Exceptions;
using RailworksForge.Core.External;
using RailworksForge.Core.Models.Common;

namespace RailworksForge.Core.Models;

public class ConsistEntry
{
    public required Blueprint Blueprint { get; init; }

    public required bool Flipped { get; init; }

    private string BinaryXmlPath => BinaryPath.Replace(".bin", ".bin.xml");

    public string BlueprintIdPath => Blueprint.BlueprintId.Replace('\\', '/').Replace(".xml", ".bin");

    public string ProductPath => Path.Join(Paths.GetAssetsDirectory(), Blueprint.BlueprintSetIdProvider, Blueprint.BlueprintSetIdProduct);

    public string BinaryPath => Path.Join(ProductPath, BlueprintIdPath);

    private IDocument? _xmlDocument;

    public async Task<IDocument> GetXmlDocument()
    {
        if (_xmlDocument is not null)
        {
            return _xmlDocument;
        }

        var path = await ConvertBinToXml();
        var sensitivePath = Paths.GetActualPathFromInsensitive(path);

        if (sensitivePath is null)
        {
            throw new Exception($"failed to find part of path {path}");
        }

        var text = await File.ReadAllTextAsync(sensitivePath);
        var document = await XmlParser.ParseDocumentAsync(text);

        XmlException.ThrowIfNotExists(document, path);

        _xmlDocument = document;

        return document;
    }

    private async Task<string> ConvertBinToXml()
    {
        if (File.Exists(BinaryXmlPath))
        {
            return BinaryXmlPath;
        }

        var inputPath = HasBinary ? BinaryPath : ExtractBinary();
        var result = await Serz.Convert(inputPath);

        return result.OutputPath;
    }

    private string ExtractBinary()
    {
        var destination = Path.Join(ProductPath, BlueprintIdPath);
        var archives = Directory.EnumerateFiles(ProductPath, "*.ap");
        var archivePath = BlueprintIdPath.StartsWith('/') ? BlueprintIdPath.Remove(1) : BlueprintIdPath;

        foreach (var archive in archives)
        {
            Archives.ExtractFileContentFromPath(archive, archivePath, destination);
        }

        return destination;
    }

    private bool HasBinary => File.Exists(BinaryPath);
}
