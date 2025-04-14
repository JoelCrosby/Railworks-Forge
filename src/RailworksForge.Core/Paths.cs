using System.Runtime.InteropServices;
using System.Security.Cryptography;

using AngleSharp.Text;

using Microsoft.Win32;

using RailworksForge.Core.Config;
using RailworksForge.Core.Exceptions;
using RailworksForge.Core.Extensions;

namespace RailworksForge.Core;

public static class Paths
{
    private enum Platform
    {
        Linux = 0,
        FreeBsd = 1,
        Osx = 2,
        Windows = 3,
    }

    private const string PrimaryDirName = nameof(RailworksForge);

    private const string InstallPathRegKey = @"KEY_LOCAL_MACHINE\Software\Wow6432Node\Railsimulator.com\RailWorks";
    private const string InstallPathRegKeyValueName = "Install_Path";

    private static Platform GetPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Platform.Windows;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return Platform.Osx;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
        {
            return Platform.FreeBsd;
        }

        return Platform.Linux;
    }

    public static string? GetHomeDirectory() => GetPlatform() switch
    {
        Platform.Linux => Environment.GetEnvironmentVariable("HOME"),
        Platform.Osx => Environment.GetEnvironmentVariable("HOME"),
        Platform.FreeBsd => Environment.GetEnvironmentVariable("HOME"),
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

    public static string GetGameDirectory()
    {
        if (GetPlatform() == Platform.Windows)
        {
            var entry = Registry.GetValue(InstallPathRegKey, InstallPathRegKeyValueName, null);

            if (entry is string path)
            {
                if (Paths.Exists(path)) return path;
            }
        }

        return Configuration.Get().GameDirectoryPath;
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

    private static string? _routesDirectory;

    public static string GetRoutesDirectory()
    {
        if (string.IsNullOrEmpty(_routesDirectory))
        {
            _routesDirectory = Path.Join(GetGameDirectory(), "Content", "Routes");
        }

        return _routesDirectory;
    }

    public static string ToWindowsPath(this string path)
    {
        return path.Replace("/", @"\").ReplaceFirst(@"\", @"z:\");
    }

    public static bool Exists(string path, string? rootPath = null)
    {
        return GetActualPathFromInsensitive(path, rootPath) is not null;
    }

    public static string? GetActualPathFromInsensitive(string path, string? rootPath = null)
    {
        if (GetPlatform() is Platform.Windows && Path.Exists(path))
        {
            return path;
        }

        var normalisedPath = path.Replace('\\', Path.DirectorySeparatorChar);

        if (Path.Exists(normalisedPath))
        {
            return normalisedPath;
        }

        if (normalisedPath is null)
        {
            throw new Exception("unable to get relative path.");
        }

        var relative = rootPath is not null ? normalisedPath.Replace(rootPath, string.Empty) : normalisedPath;
        var parts = relative.Split(Path.DirectorySeparatorChar).Where(r => !string.IsNullOrEmpty(r));
        var partsQueue = new Queue<string>(parts);
        var basePath = rootPath ?? "/";

        while (partsQueue.TryDequeue(out var part))
        {
            var entries = Directory.EnumerateFileSystemEntries(basePath, "*");
            var partPath = Path.Join(basePath, part);

            if (entries.FirstOrDefault(e => string.Equals(e, partPath, StringComparison.OrdinalIgnoreCase)) is {} entry)
            {
                basePath = entry;
            }
            else
            {
                return null;
            }
        }

        return basePath;
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

    public static string GetArchiveCachePath(string path)
    {
        var renamedOutput = path.Replace(".ap", string.Empty);
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
