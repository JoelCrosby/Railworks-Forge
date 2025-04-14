using System.IO.Compression;

using Avalonia.Media.Imaging;

using RailworksForge.Core.Exceptions;
using RailworksForge.Core.Extensions;

using Serilog;

namespace RailworksForge.Core;

public static class Archives
{


    private static readonly Lock GetBitmapSyncObj = new ();
    private static readonly Lock EntryExistsSyncObj = new ();

    public static string GetTextFileContentFromPath(string archivePath, string filePath)
    {
        using var archive = ZipFile.OpenRead(archivePath);

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
        using var archive = ZipFile.OpenRead(archivePath);

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

    public static Bitmap? GetBitmapStreamFromPath(string archivePath, string filePath)
    {
        var normalisedArchivePath = archivePath.NormalisePath();

        lock (GetBitmapSyncObj)
        {
            if (Cache.ImageCache.GetValueOrDefault((archivePath, filePath)) is {} cachedBitmap)
            {
                Log.Information("loaded image from cache {Archive} {Path} as cache hit", archivePath.ToRelativeGamePath(), filePath);

                return cachedBitmap;
            }

            var unixFilePath = filePath.Replace('\\', '/');
            var archiveEntryFilepath = unixFilePath.StartsWith('/') ? unixFilePath.TrimStart('/') : unixFilePath;
            var normalisedEntryFilepath = archiveEntryFilepath.NormalisePath();

            if (Cache.ArchiveCache.GetValueOrDefault(normalisedArchivePath) is {} cachedArchive)
            {
                if (!cachedArchive.Any(e => string.Equals(e, normalisedEntryFilepath, StringComparison.OrdinalIgnoreCase)))
                {
                    Log.Information("skipped indexing archive {Archive} as cache hit", archivePath.ToRelativeGamePath());

                    return null;
                }
            }

            using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Read);
            var entry = archive.Entries.FirstOrDefault(entry => string.Equals(entry.FullName, archiveEntryFilepath, StringComparison.OrdinalIgnoreCase));

            if (entry is null)
            {
                return null;
            }

            var stream = entry.Open();

            var image = new MemoryStream();
            stream.CopyTo(image);
            image.Position = 0;

            var result = image.ReadBitmap();

            Cache.ImageCache.TryAdd((archivePath, filePath), result);

            return result;
        }
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

    public static List<string> ExtractFilesOfType(string archivePath, string extension)
    {
        using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Read);

        var entries = archive.Entries.Where(entry => entry.Name.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
        var destinationDir = Paths.GetArchiveCachePath(archivePath);

        Directory.CreateDirectory(destinationDir);
        DirectoryException.ThrowIfNotExists(destinationDir);

        var output = new List<string>();

        foreach (var entry in entries)
        {
            var destinationFile = Path.Combine(destinationDir, entry.FullName);
            var destinationFileDir = Path.GetDirectoryName(destinationFile);

            DirectoryException.ThrowIfNotExists(destinationFileDir);
            Directory.CreateDirectory(destinationFileDir);

            entry.ExtractToFile(destinationFile, true);

            output.Add(destinationFile);
        }

        return output;
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

    public static bool TopLevelDirectoryExists(string archivePath, string directoryName)
    {
        var normalizedArchivePath = archivePath.NormalisePath();
        if (Cache.ArchiveCache.GetValueOrDefault(normalizedArchivePath) is {} cachedArchive)
        {
            var normalisedDirectoryName = directoryName.NormalisePath();

            if (cachedArchive.Any(e => string.Equals(e, normalisedDirectoryName, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        var entries = GetEntries(archivePath);
        return entries.Any(e => e.StartsWith(directoryName, StringComparison.OrdinalIgnoreCase));
    }

    private static readonly HashSet<string> CorruptArchivePaths = ["Assets/DTG/Academy/AcademyAssetsTest.ap"];

    public static bool EntryExists(string archivePath, string agnosticBlueprintIdPath)
    {
        var normalisedArchivePath = archivePath.NormalisePath();
        var normalisedBlueprintPath = agnosticBlueprintIdPath.NormalisePath();
        var cachedArchiveFiles = Cache.ArchiveCache.GetValueOrDefault(normalisedArchivePath);

        if (cachedArchiveFiles?.Contains(normalisedBlueprintPath) ?? false)
        {
            return true;
        }

        if (CorruptArchivePaths.Any(a => normalisedArchivePath.Contains(a)))
        {
            return false;
        }


        lock (EntryExistsSyncObj)
        {
            Log.Information("checking entry exists {ArchivePath} {Entry}", archivePath.ToRelativeGamePath(), agnosticBlueprintIdPath);

            var entries = GetEntries(archivePath);
            return entries.Any(e => string.Equals(e, normalisedBlueprintPath, StringComparison.OrdinalIgnoreCase));
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

    private static HashSet<string> GetEntries(string archivePath)
    {
        Log.Information("indexing archive {Archive}", archivePath.ToRelativeGamePath());

        using var archive = ZipFile.OpenRead(archivePath);
        var entries = archive.Entries.Select(e => e.FullName.NormalisePath()).ToHashSet();

        Cache.ArchiveCache.TryAdd(archivePath.NormalisePath(), entries);

        return entries;
    }
}
