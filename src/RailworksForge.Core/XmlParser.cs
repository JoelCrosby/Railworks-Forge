using AngleSharp.Xml.Dom;
using AngleSharp.Xml.Parser;

namespace RailworksForge.Core;

public class XmlParser
{
    private static readonly XmlParserOptions Options = new()
    {
        IsSuppressingErrors = false,
        IsKeepingSourceReferences = true,
    };

    public static IXmlDocument ParseDocument(string source)
    {
        return new AngleSharp.Xml.Parser.XmlParser(Options).ParseDocument(source);
    }

    public static Task<IXmlDocument> ParseDocumentAsync(string source)
    {
        return new AngleSharp.Xml.Parser.XmlParser(Options).ParseDocumentAsync(source, default);
    }
}
