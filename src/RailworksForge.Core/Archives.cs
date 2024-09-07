using System.IO.Compression;

using Avalonia.Media.Imaging;

using RailworksForge.Core.Exceptions;
using RailworksForge.Core.Extensions;

namespace RailworksForge.Core;

public static class Archives
{
    public static string GetTextFileContentFromPath(string archivePath, string filePath)
    {
        using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Read);

        var normalisedFilepath = filePath.StartsWith('/') ? filePath.TrimStart('/') : filePath;
        var entry = archive.Entries.FirstOrDefault(entry => string.Equals(entry.FullName, normalisedFilepath, StringComparison.OrdinalIgnoreCase));

        if (entry is null)
        {
            ArchiveException.ThrowFileNotFound(archivePath, filePath);
        }

        var content = entry.Open();

        using var reader = new StreamReader(content);

        return reader.ReadToEnd();
    }

    public static string? TryGetTextFileContentFromPath(string archivePath, string filePath)
    {
        using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Read);

        var normalisedFilepath = filePath.StartsWith('/') ? filePath.TrimStart('/') : filePath;
        var entry = archive.Entries.FirstOrDefault(entry => string.Equals(entry.FullName, normalisedFilepath, StringComparison.OrdinalIgnoreCase));

        if (entry is null)
        {
            return null;
        }

        var content = entry.Open();

        using var reader = new StreamReader(content);

        return reader.ReadToEnd();
    }

    public static async Task<Bitmap?> GetBitmapStreamFromPath(string archivePath, string filePath, bool strict = true)
    {
        using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Read);

        var normalisedFilepath = filePath.StartsWith('/') ? filePath.TrimStart('/') : filePath;
        var entry = archive.Entries.FirstOrDefault(entry => string.Equals(entry.FullName, normalisedFilepath, StringComparison.OrdinalIgnoreCase));

        if (entry is null)
        {
            if (strict)
            {
                ArchiveException.ThrowFileNotFound(archivePath, filePath);
            }

            return null;
        }

        var stream = entry.Open();

        var image = new MemoryStream();
        await stream.CopyToAsync(image);
        image.Position = 0;

        return image.ReadBitmap();
    }

    public static bool ExtractFileContentFromPath(string archivePath, string filePath, string destination)
    {
        using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Read);

        var entry = archive.Entries.FirstOrDefault(entry => entry.FullName == filePath);

        if (entry is null)
        {
            return false;
        }

        var destinationDir = Path.GetDirectoryName(destination);

        DirectoryException.ThrowIfNotExists(destinationDir);
        Directory.CreateDirectory(destinationDir);

        entry.ExtractToFile(destination, true);

        return true;
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

            DirectoryException.ThrowIfNotExists(destinationDir);
            Directory.CreateDirectory(destinationDir);

            entry.ExtractToFile(destination, true);
        }
    }

    public static bool EntryExists(string archivePath, string agnosticBlueprintIdPath)
    {
        var normalisedArchivePath = archivePath.NormalisePath();
        var normalisedBlueprintPath = agnosticBlueprintIdPath.NormalisePath();
        var cachedArchiveFiles = Cache.ArchiveFileCache.GetValueOrDefault(normalisedArchivePath);

        if (cachedArchiveFiles?.Contains(normalisedBlueprintPath) ?? false)
        {
            return true;
        }

        try
        {
            using var archive = ZipFile.OpenRead(archivePath);

            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.Contains('.', StringComparison.Ordinal) is false)
                {
                    continue;
                }

                var normalisedEntry = entry.FullName.NormalisePath();

                if (Cache.ArchiveFileCache.GetValueOrDefault(normalisedArchivePath) is {} files)
                {
                    files.Add(normalisedEntry);
                }
                else
                {
                    // ReSharper disable once UnusedParameter.Local
                    HashSet<string> UpdateEntry(string _, HashSet<string> value)
                    {
                        value.Add(normalisedEntry);
                        return value;
                    }

                    HashSet<string> CreateEntry(string _) => [normalisedEntry];

                    Cache.ArchiveFileCache.AddOrUpdate(normalisedArchivePath, CreateEntry,  UpdateEntry);
                }
            }

            return Cache.ArchiveFileCache[normalisedArchivePath].Contains(normalisedBlueprintPath);
        }
        catch (Exception e)
        {
            ArchiveException.ThrowReadFailedForArchive(e, archivePath);

            throw new InvalidOperationException();
        }
    }

    public static List<string> ListFilesInPath(string archivePath, string directoryPath, string extension)
    {
        using var archive = ZipFile.OpenRead(archivePath);

        return archive.Entries
            .Where(entry => entry.FullName.StartsWith(directoryPath) && entry.FullName.EndsWith(extension))
            .Select(entry => entry.FullName)
            .ToList();
    }
}
