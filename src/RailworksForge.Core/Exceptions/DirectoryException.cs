using System.Diagnostics.CodeAnalysis;

namespace RailworksForge.Core.Exceptions;

public class DirectoryException(string message) : Exception(message)
{
    public static void ThrowIfNotExists([NotNull] string? path)
    {
        if (path is not null) return;

        throw new DirectoryException("an error occured while attempting to read directory information");
    }
}
