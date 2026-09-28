using System.IO.Compression;

using RailworksForge.Core.Exceptions;
using RailworksForge.Core.Extensions;

using Serilog;

namespace RailworksForge.Core;

public static class Archives
{


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

        using var content = entry.Open();
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

        using var content = entry.Open();
        using var reader = new StreamReader(content);

        return reader.ReadToEnd();
    }

    public static byte[]? ReadFileBytes(string archivePath, string filePath)
    {
        try
        {
            var normalisedArchivePath = archivePath.NormalisePath();
            var unixFilePath = filePath.Replace('\\', '/');
            var archiveEntryFilepath = unixFilePath.TrimStart('/');
            var normalisedEntryFilepath = archiveEntryFilepath.NormalisePath();
            var isKnownMissing = GetCachedEntries(archivePath, normalisedArchivePath) is {} cachedArchive
                && !cachedArchive.Contains(normalisedEntryFilepath);

            if (isKnownMissing)
            {
                return null;
            }

            using var archive = ZipFile.OpenRead(archivePath);
            var entry = archive.Entries.FirstOrDefault(entry =>
                string.Equals(entry.FullName, archiveEntryFilepath, StringComparison.OrdinalIgnoreCase));

            if (entry is null)
            {
                return null;
            }

            Log.Debug("read {Path} from archive {Archive}", filePath, archivePath.ToRelativeGamePath());

            using var stream = entry.Open();
            using var content = new MemoryStream();

            stream.CopyTo(content);

            return content.ToArray();
        }
        catch
        {
            return null;
        }
    }

    public static bool ExtractFileContentFromPath(string archivePath, string filePath, string destination)
    {
        using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Read);

        var entryPath = filePath.Replace('\\', '/').TrimStart('/');
        var entry = archive.Entries.FirstOrDefault(entry => string.Equals(entry.FullName, entryPath, StringComparison.OrdinalIgnoreCase));

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
        return ExtractEntriesToCache(archivePath, entryPath => entryPath.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
    }

    // Extracts into the app's cache rather than beside the archive, so the game's own folders are left untouched.
    // ExtractToFile stamps the entry's modified time on the file, so a matching size and time means it is current.
    public static List<string> ExtractEntriesToCache(string archivePath, Func<string, bool> includeEntry)
    {
        using var archive = ZipFile.OpenRead(archivePath);

        var destinationDir = Paths.GetArchiveCachePath(archivePath);
        var output = new List<string>();

        Directory.CreateDirectory(destinationDir);

        foreach (var entry in archive.Entries)
        {
            var isFile = !entry.FullName.EndsWith('/');

            if (!isFile || !includeEntry(entry.FullName))
            {
                continue;
            }

            var destinationFile = Paths.ResolveWithin(destinationDir, entry.FullName);
            var existing = new FileInfo(destinationFile);
            var isAlreadyExtracted = existing.Exists
                && existing.Length == entry.Length
                && existing.LastWriteTime == entry.LastWriteTime.DateTime;

            if (!isAlreadyExtracted)
            {
                Directory.CreateDirectory(existing.DirectoryName!);
                entry.ExtractToFile(destinationFile, true);
            }

            output.Add(destinationFile);
        }

        return output;
    }

    public static void ExtractDirectory(string archivePath, string directoryPath)
    {
        using var archive = ZipFile.OpenRead(archivePath);

        var containingDirectory = Path.GetDirectoryName(archivePath);
        var directoryPrefix = directoryPath.Replace('\\', '/').TrimEnd('/') + "/";
        var entries = archive.Entries.Where(entry =>
        {
            var isFile = !entry.FullName.EndsWith('/');
            var isInDirectory = entry.FullName.StartsWith(directoryPrefix, StringComparison.OrdinalIgnoreCase);

            return isFile && isInDirectory;
        });

        if (containingDirectory is null)
        {
            ArchiveException.ThrowDirectoryNotFound(archivePath, directoryPath);
        }

        foreach (var entry in entries)
        {
            var destination = Paths.ResolveWithin(containingDirectory, entry.FullName);
            var destinationDir = Path.GetDirectoryName(destination);

            DirectoryException.ThrowIfNotExists(destinationDir);
            Directory.CreateDirectory(destinationDir);

            entry.ExtractToFile(destination, true);
        }
    }

    public static bool TopLevelDirectoryExists(string archivePath, string directoryName)
    {
        var entries = GetEntries(archivePath);
        var directoryPrefix = directoryName.NormalisePath().TrimEnd('/') + "/";

        return entries.Any(e => e.StartsWith(directoryPrefix, StringComparison.Ordinal));
    }

    private static readonly HashSet<string> CorruptArchivePaths = ["assets/dtg/academy/academyassetstest.ap"];

    private static bool IsCorruptArchive(string normalisedArchivePath)
    {
        return CorruptArchivePaths.Any(normalisedArchivePath.Contains);
    }

    public static bool EntryExists(string archivePath, string agnosticBlueprintIdPath)
    {
        lock (EntryExistsSyncObj)
        {
            var normalisedArchivePath = archivePath.NormalisePath();
            var normalisedBlueprintPath = agnosticBlueprintIdPath.NormalisePath();
            var cachedArchiveFiles = GetCachedEntries(archivePath, normalisedArchivePath);

            if (cachedArchiveFiles is not null)
            {
                Log.Debug("cache archive found {ArchivePath}", archivePath.ToRelativeGamePath());

                if (cachedArchiveFiles.Contains(normalisedBlueprintPath))
                {
                    return true;
                }

                Log.Debug("cache entry not found {ArchivePath} {Entry}", archivePath.ToRelativeGamePath(), agnosticBlueprintIdPath);

                return false;
            }

            if (IsCorruptArchive(normalisedArchivePath))
            {
                return false;
            }

            Log.Debug("checking entry exists {ArchivePath} {Entry}", archivePath.ToRelativeGamePath(), agnosticBlueprintIdPath);

            var entries = GetEntries(archivePath);
            return entries.Contains(normalisedBlueprintPath);
        }
    }

    public static List<string> ListFilesInPath(string archivePath, string directoryPath, string extension)
    {
        using var archive = ZipFile.OpenRead(archivePath);

        return archive.Entries
            .Where(entry =>
            {
                var isInDirectory = entry.FullName.StartsWith(directoryPath, StringComparison.OrdinalIgnoreCase);
                var hasExtension = entry.FullName.EndsWith(extension, StringComparison.OrdinalIgnoreCase);

                return isInDirectory && hasExtension;
            })
            .Select(entry => entry.FullName)
            .ToList();
    }

    private static HashSet<string> GetEntries(string archivePath)
    {
        lock (EntryExistsSyncObj)
        {
            if (IsCorruptArchive(archivePath.NormalisePath()))
            {
                return [];
            }

            var normalisedArchivePath = archivePath.NormalisePath();

            if (GetCachedEntries(archivePath, normalisedArchivePath) is {} cachedArchive)
            {
                return cachedArchive;
            }

            Log.Debug("indexing archive {Archive}", archivePath.ToRelativeGamePath());

            using var archive = ZipFile.OpenRead(archivePath);
            var entries = archive.Entries.Select(e => e.FullName.NormalisePath()).ToHashSet();

            Cache.ArchiveCache[normalisedArchivePath] = new ArchiveIndex(File.GetLastWriteTimeUtc(archivePath), entries);

            return entries;
        }
    }

    private static HashSet<string>? GetCachedEntries(string archivePath, string normalisedArchivePath)
    {
        if (!Cache.ArchiveCache.TryGetValue(normalisedArchivePath, out var index))
        {
            return null;
        }

        var isCurrent = index.LastWriteTimeUtc == File.GetLastWriteTimeUtc(archivePath);

        return isCurrent ? index.Entries : null;
    }
}
