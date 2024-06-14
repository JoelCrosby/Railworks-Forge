using System.Diagnostics.CodeAnalysis;

namespace RailworksForge.Core.Exceptions;

public class ArchiveException(string message) : Exception(message)
{
    [DoesNotReturn]
    public static void ThrowFileNotFound(string archivePath, [NotNull] string filename)
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
}
