using AngleSharp.Xml.Dom;

using RailworksForge.Core.Exceptions;
using RailworksForge.Core.External;

namespace RailworksForge.Core.Models;

public class ConsistEntry
{
    public required string BlueprintId { get; init; }

    public required string BlueprintIdProduct { get; init; }

    public required string BlueprintIdProvider { get; init; }

    public required bool Flipped { get; init; }

    private string BinaryXmlPath => BinaryPath.Replace(".bin", ".bin.xml");

    public string BlueprintIdPath => BlueprintId.Replace('\\', '/').Replace(".xml", ".bin");

    public string ProductPath => Path.Join(Paths.GetAssetsDirectory(), BlueprintIdProvider, BlueprintIdProduct);

    public string BinaryPath => Path.Join(ProductPath, BlueprintIdPath);

    private IXmlDocument? _xmlDocument;

    public async Task<IXmlDocument> GetXmlDocument()
    {
        if (_xmlDocument is not null)
        {
            return _xmlDocument;
        }

        var path = await ConvertBinToXml();
        var text = await File.ReadAllTextAsync(path);
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
        var outputPath = inputPath.Replace(".bin", ".bin.xml");

        await Serz.Convert(inputPath);

        return outputPath;
    }

    private string ExtractBinary()
    {
        var destination = Path.Join(ProductPath, BlueprintIdPath.Replace(".bin", ".bin.xml"));
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
