using System.Diagnostics.CodeAnalysis;

using AngleSharp.Dom;

namespace RailworksForge.Core.Exceptions;

public class XmlException(string message) : Exception(message)
{
    public static void ThrowIfNotExists([NotNull] IDocument? document, string path)
    {
        if (document is not null) return;

        throw new XmlException("failed to parse xml document at {path}");
    }

    [DoesNotReturn]
    public static void ThrowInvalidNode(IElement element, string message)
    {
        var innerMessage = $"{message} : {element.TagName}";

        throw new XmlException(innerMessage);
    }
}
