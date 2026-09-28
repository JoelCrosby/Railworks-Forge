using AngleSharp.Dom;

using RailworksForge.Core.Exceptions;
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

        var looseXmlPath = Paths.GetActualPathFromInsensitive(BinaryXmlPath);

        _xmlDocument = looseXmlPath is not null
            ? await ParseXmlFile(looseXmlPath)
            : await Blueprint.GetXmlDocument();

        return _xmlDocument;
    }

    private static async Task<IDocument> ParseXmlFile(string path)
    {
        await using var file = File.OpenRead(path);
        var document = await XmlParser.ParseDocumentAsync(file);

        XmlException.ThrowIfNotExists(document, path);

        return document;
    }
}
