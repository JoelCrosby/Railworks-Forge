using AngleSharp.Xml.Dom;

namespace RailworksForge.Core;

public class XmlParser
{
    public static IXmlDocument ParseDocument(string source)
    {
        return new AngleSharp.Xml.Parser.XmlParser().ParseDocument(source);
    }

    public static Task<IXmlDocument> ParseDocumentAsync(string source)
    {
        return new AngleSharp.Xml.Parser.XmlParser().ParseDocumentAsync(source, default);
    }
}
