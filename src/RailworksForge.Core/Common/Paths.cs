using System.Runtime.Versioning;
using System.Security.Cryptography;

using AngleSharp.Text;

using Microsoft.Win32;

using RailworksForge.Core.Config;
using RailworksForge.Core.Exceptions;
using RailworksForge.Core.Extensions;
using RailworksForge.Core.Routes;

namespace RailworksForge.Core.Common;

public static class Paths
{
    public enum Platform
    {
        Linux = 0,
        Osx = 1,
        Windows = 2,
    }

    private const string PrimaryDirName = nameof(RailworksForge);

    private const string InstallPathRegKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Railsimulator.com\RailWorks";
    private const string InstallPathRegKeyValueName = "Install_Path";

    public static Platform GetPlatform()
    {
        if (OperatingSystem.IsWindows())
        {
            return Platform.Windows;
        }

        if (OperatingSystem.IsMacOS())
        {
            return Platform.Osx;
        }

        return Platform.Linux;
    }

    public static string? GetHomeDirectory() => GetPlatform() switch
    {
        Platform.Linux => Environment.GetEnvironmentVariable("HOME"),
        Platform.Osx => Environment.GetEnvironmentVariable("HOME"),
        Platform.Windows => GetHomeDirectoryWindows(),
        _ => throw new PlatformNotSupportedException(),
    };

    private static string GetHomeDirectoryWindows()
    {
        var homeDrive = Environment.GetEnvironmentVariable("HOMEDRIVE");
        var homePath = Environment.GetEnvironmentVariable("HOMEPATH");

        return Path.Join(homeDrive, homePath);
    }

    public static string GetConfigurationFolder()
    {
        var home = GetHomeDirectory();
        const string configFolder = ".config";

        return Path.Join(home, configFolder, PrimaryDirName);
    }

    public static string GetCacheFolder()
    {
        var home = GetHomeDirectory();
        const string configFolder = ".cache";

        return Path.Join(home, configFolder, PrimaryDirName);
    }

    private static string _gameDirectory = string.Empty;
    private static readonly Lock RegLock = new();

    public static string GetGameDirectory()
    {
        return _gameDirectory;
    }

    public static void SetGameDirectory()
    {
        var configuredPath = Configuration.Get().GameDirectoryPath;
        var hasConfiguredPath = IsValidGameDirectory(configuredPath);

        if (hasConfiguredPath)
        {
            _gameDirectory = configuredPath;

            return;
        }

        if (OperatingSystem.IsWindows())
        {
            lock (RegLock)
            {

                if (GetGameDirectoryWindows() is {} windowsPath)
                {
                    _gameDirectory = windowsPath;

                    return;
                }
            }
        }

        _gameDirectory = configuredPath;
    }

    public static bool IsValidGameDirectory(string? path)
    {

        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return false;
        }

        var hasAssets = Exists(Path.Join(path, "Assets"), path);
        var hasContent = Exists(Path.Join(path, "Content"), path);

        return hasAssets && hasContent;
    }

    [SupportedOSPlatform("windows")]
    private static string? GetGameDirectoryWindows()
    {
         var entry = Registry.GetValue(InstallPathRegKey, InstallPathRegKeyValueName, null);

         if (entry is not string path) return null;

         return Exists(path) ? path : null;
    }

    private static string? _assetsDirectory;

    public static string GetAssetsDirectory()
    {
        if (string.IsNullOrEmpty(_assetsDirectory))
        {
            _assetsDirectory = Path.Join(GetGameDirectory(), "Assets");
        }

        return _assetsDirectory;
    }

    // Guards against archive entries such as "..\..\.bashrc" writing outside the directory being extracted into.
    public static string ResolveWithin(string rootDirectory, string relativePath)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootDirectory));
        var normalisedRelativePath = relativePath.Replace('\\', '/').TrimStart('/');
        var fullPath = Path.GetFullPath(Path.Join(root, normalisedRelativePath));
        var isInsideRoot = fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal);

        if (!isInsideRoot)
        {
            throw new InvalidDataException($"path '{relativePath}' resolves outside '{rootDirectory}'");
        }

        return fullPath;
    }

    public static string GetContentDirectory()
    {
        return Path.Join(GetGameDirectory(), "Content");
    }

    private static string? _routesDirectory;

    public static string GetRoutesDirectory()
    {
        if (string.IsNullOrEmpty(_routesDirectory))
        {
            _routesDirectory = Path.Join(GetContentDirectory(), "Routes");
        }

        return _routesDirectory;
    }

    public static string ToWindowsPath(this string path)
    {
        if (GetPlatform() is Platform.Windows) return path;

        return path.Replace("/", @"\").ReplaceFirst(@"\", @"z:\");
    }

    public static bool Exists(string path, string? rootPath = null)
    {
        // The file system already ignores case here, and listing each directory to find the real casing is slow.
        if (IsFileSystemCaseInsensitive)
        {
            return Path.Exists(NormaliseSeparators(path));
        }

        rootPath ??= GetGameDirectory();
        return GetActualPathFromInsensitive(path, rootPath) is not null;
    }

    // Windows and macOS match file names case-insensitively, so an existing path can still have the wrong case.
    private static readonly bool IsFileSystemCaseInsensitive = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();

    private static string NormaliseSeparators(string path)
    {
        return path
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);
    }

    private static readonly EnumerationOptions CaseInsensitiveEntryOptions = new()
    {
        MatchCasing = MatchCasing.CaseInsensitive,
        MatchType = MatchType.Simple,
        IgnoreInaccessible = true,
        AttributesToSkip = 0,
    };

    // Resolves a path whose components may have the wrong case, returning the casing used on disk.
    public static string? GetActualPathFromInsensitive(string path, string? rootPath = null)
    {
        var normalisedPath = NormaliseSeparators(path);
        var exists = Path.Exists(normalisedPath);

        if (!IsFileSystemCaseInsensitive && exists)
        {
            return normalisedPath;
        }

        // A case-insensitive file system has already looked for every casing, so a miss needs no directory listings.
        if (IsFileSystemCaseInsensitive && !exists)
        {
            return null;
        }

        var root = string.IsNullOrEmpty(rootPath) ? null : Path.TrimEndingDirectorySeparator(rootPath);
        var isUnderRoot = root is not null
            && normalisedPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        var basePath = isUnderRoot ? root! : Path.GetPathRoot(normalisedPath) ?? string.Empty;
        var relative = normalisedPath[basePath.Length..];
        var parts = relative.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var exactPath = Path.Join(basePath, part);

            // On a case-sensitive file system an exact hit already has the right case, so the directory listing is skipped.
            if (!IsFileSystemCaseInsensitive && Path.Exists(exactPath))
            {
                basePath = exactPath;
                continue;
            }

            if (!Directory.Exists(basePath))
            {
                return null;
            }

            if (FindEntryIgnoringCase(basePath, part) is {} match)
            {
                basePath = match;
                continue;
            }

            // Windows also resolves 8.3 short names such as RUNNER~1, which no listed entry is called.
            if (IsFileSystemCaseInsensitive && Path.Exists(exactPath))
            {
                basePath = exactPath;
                continue;
            }

            return null;
        }

        return basePath;
    }

    private static string? FindEntryIgnoringCase(string directory, string name)
    {
        var hasWildcard = name.AsSpan().IndexOfAny('*', '?') >= 0;
        var entries = hasWildcard
            ? Directory.EnumerateFileSystemEntries(directory)
            : Directory.EnumerateFileSystemEntries(directory, name, CaseInsensitiveEntryOptions);

        return entries.FirstOrDefault(entry => string.Equals(Path.GetFileName(entry), name, StringComparison.OrdinalIgnoreCase));
    }

    private static byte[] CalculateMd5Hash(string path)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(path);

        return md5.ComputeHash(stream);
    }

    public static async Task CreateMd5HashFile(string path)
    {
        var hash = CalculateMd5Hash(path);
        var filename = $"{Path.GetFileName(path)}.MD5";

        var directory = Directory.GetParent(path)?.FullName;

        if (directory is null)
        {
            throw new Exception($"could not find parent directory for path {path}");
        }

        var output = Path.Join(directory, filename);

        File.Delete(output);

        await File.WriteAllBytesAsync(output, hash);
    }

    public static string GetLoggingPath()
    {
        return Path.Join(GetConfigurationFolder(), "logs");
    }

    public static readonly string CacheOutputPath = Path.Join(GetConfigurationFolder(), "xml-cache");

    public static readonly string PackageCachePath = Path.Join(GetConfigurationFolder(), "package-cache");

    public static string GetRouteAssetsCachePath(Route route)
    {
        return Path.Join(GetConfigurationFolder(), "route-assets-cache", $"{route.Id}.csv");
    }

    public static string GetAssetCachePath(string path, bool isBin)
    {
        var renamedOutput = isBin ? path.Replace(".bin", ".bin.xml") : path.Replace(".bin.xml", ".bin");
        var flattened = renamedOutput.Replace(GetGameDirectory(), string.Empty);

        var outputPath = Path.Join(CacheOutputPath, flattened);
        var parentDir = Directory.GetParent(outputPath)?.FullName;

        DirectoryException.ThrowIfNotExists(parentDir);
        Directory.CreateDirectory(parentDir);

        return outputPath;
    }

    public const string ArchivePathPreserveSuffix = "__RFAP__";

    public static string GetArchiveCachePath(string path)
    {
        var renamedOutput = path.Replace(".ap", ArchivePathPreserveSuffix);
        var flattened = renamedOutput.Replace(GetGameDirectory(), string.Empty);

        var outputPath = Path.Join(CacheOutputPath, flattened);
        var parentDir = Directory.GetParent(outputPath)?.FullName;

        DirectoryException.ThrowIfNotExists(parentDir);
        Directory.CreateDirectory(parentDir);

        return outputPath;
    }

    public static List<DirectoryInfo> GetAssetProviders()
    {
        return Directory
            .EnumerateDirectories(GetAssetsDirectory(), "*", SearchOption.TopDirectoryOnly)
            .ToDirectoryInfoList();

    }

    public static List<DirectoryInfo> GetAssetProviderProducts(string provider)
    {
        var directory = Path.Join(GetAssetsDirectory(), provider);

        if (!Exists(directory))
        {
            throw new DirectoryNotFoundException($"Directory {directory} not found");
        }

        return Directory.EnumerateDirectories(directory, "*", SearchOption.TopDirectoryOnly)
            .ToDirectoryInfoList();
    }
}
