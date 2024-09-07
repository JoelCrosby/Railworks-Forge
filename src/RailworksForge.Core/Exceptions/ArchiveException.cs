using System.Diagnostics.CodeAnalysis;

namespace RailworksForge.Core.Exceptions;

public class ArchiveException : Exception
{
    private ArchiveException(string message) : base(message)
    {
    }

    private ArchiveException(Exception inner, string message) : base(message, inner)
    {
    }

    [DoesNotReturn]
    public static void ThrowFileNotFound(string archivePath, string filename)
    {
        throw new ArchiveException(
            $"failed to find file with name {filename} in compressed archive {archivePath}"
        );
    }

    [DoesNotReturn]
    public static void ThrowDirectoryNotFound(string archivePath, string directory)
    {
        throw new ArchiveException(
            $"failed to find directory with name {directory} in compressed archive {archivePath}"
        );
    }

    [DoesNotReturn]
    public static void ThrowReadFailedForArchive(Exception inner, string archivePath)
    {
        throw new ArchiveException(inner,
            $"failed to open archive with path {archivePath}"
        );
    }
}
