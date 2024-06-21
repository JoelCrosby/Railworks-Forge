using System.Diagnostics.CodeAnalysis;

using AngleSharp.Xml.Dom;

namespace RailworksForge.Core.Exceptions;

public class XmlException(string message) : Exception(message)
{
    public static void ThrowIfNotExists([NotNull] IXmlDocument? document, string path)
    {
        if (document is not null) return;

        throw new DirectoryException("failed to parse xml document at {path}");
    }
}
