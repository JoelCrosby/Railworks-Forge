using AngleSharp.Dom;
using AngleSharp.Xml.Parser;

namespace RailworksForge.Core;

public class XmlParser
{
    private static readonly XmlParserOptions Options = new()
    {
        IsSuppressingErrors = false,
        IsKeepingSourceReferences = true,
    };

    public static IDocument ParseDocument(string source)
    {
        return new AngleSharp.Xml.Parser.XmlParser(Options).ParseDocument(source);
    }

    public static async Task<IDocument> ParseDocumentAsync(string source, CancellationToken cancellationToken = default)
    {
        var document = await new AngleSharp.Xml.Parser.XmlParser(Options).ParseDocumentAsync(source, cancellationToken);

        return document;
    }
}
