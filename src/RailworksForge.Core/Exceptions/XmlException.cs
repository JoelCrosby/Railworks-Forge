using System.Diagnostics.CodeAnalysis;

using AngleSharp.Html.Dom;

namespace RailworksForge.Core.Exceptions;

public class XmlException(string message) : Exception(message)
{
    public static void ThrowIfNotExists([NotNull] IHtmlDocument? document, string path)
    {
        if (document is not null) return;

        throw new DirectoryException("failed to parse xml document at {path}");
    }
}
