using System.IO.Compression;

using RailworksForge.Core.Exceptions;

namespace RailworksForge.Core;

public static class Archives
{
    public static string GetTextFileContentFromPath(string archivePath, string filePath)
    {
        using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Read);

        var entry = archive.Entries.FirstOrDefault(entry => entry.FullName == filePath);

        if (entry is null)
        {
            ArchiveException.ThrowFileNotFound(archivePath, filePath);
        }

        var content = entry.Open();

        using var reader = new StreamReader(content);

        return reader.ReadToEnd();
    }

    public static void ExtractDirectory(string archivePath, string directoryPath)
    {
        using var archive = ZipFile.OpenRead(archivePath);

        var containingDirectory = Path.GetDirectoryName(archivePath);
        var entries = archive.Entries.Where(entry => !entry.FullName.EndsWith('/') && entry.FullName.StartsWith(directoryPath));

        if (containingDirectory is null)
        {
            ArchiveException.ThrowDirectoryNotFound(archivePath, directoryPath);
        }

        foreach (var entry in entries)
        {
            var destination = Path.Join(containingDirectory, entry.FullName);
            var destinationDir = Path.GetDirectoryName(destination);

            DirectoryException.ThrowNotExists(destinationDir);
            Directory.CreateDirectory(destinationDir);

            entry.ExtractToFile(destination, true);
        }
    }
}
