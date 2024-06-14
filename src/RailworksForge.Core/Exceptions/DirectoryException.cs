using System.Diagnostics.CodeAnalysis;

namespace RailworksForge.Core.Exceptions;

public class DirectoryException(string message) : Exception(message)
{
    [DoesNotReturn]
    public static void ThrowNotExists( string? path)
    {
        throw new DirectoryException(
            "an error occured while attempting to read directory information"
        );
    }
}
