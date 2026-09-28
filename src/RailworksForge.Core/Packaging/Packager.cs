using System.IO.Compression;
using System.Text;

using Serilog;

namespace RailworksForge.Core.Packaging;

public class Packager
{
    private IProgress<InstallProgress>? _progress;

    private InstallStage _stage;
    private string _packageName = string.Empty;

    private static readonly HashSet<string> ForbiddenExtensions =
    [
        ".bak",
        ".pak",
        ".tgt",
        ".cost",
    ];

    public async Task InstallPackage(string filename, IProgress<InstallProgress> progress)
    {
        _progress = progress;

        var packageName = Path.GetFileNameWithoutExtension(filename);

        _packageName = packageName;
        RaisePackageInstallProgress(InstallStage.Installing);

        await Task.Delay(200).ConfigureAwait(false);

        var packageInfo = Path.Join(Paths.GetGameDirectory(), "PackageInfo", $"{packageName}.pi");
        var installed = Paths.Exists(packageInfo);

        if (installed)
        {
            RaisePackageInstallProgress(InstallStage.AlreadyInstalled);

            await Task.Delay(2000).ConfigureAwait(false);

            Log.Information("package {Package} already installed", packageName);
            return;
        }

        await ProcessPackage(filename);
    }

    private void RaisePackageInstallProgress(int progress, int filesProcessed, int fileCount)
    {
        var args = new InstallProgress
        {
            Stage = _stage,
            PackageName = _packageName,
            Progress = progress,
            FilesProcessed = filesProcessed,
            FileCount = fileCount,
            IsLoading = true,
        };

        _progress?.Report(args);
    }

    private void RaisePackageInstallProgress(InstallStage stage, bool isLoading = true)
    {
        _stage = stage;

        var args = new InstallProgress
        {
            Stage = _stage,
            PackageName = _packageName,
            Progress = 0,
            IsLoading = isLoading,
        };

        _progress?.Report(args);
    }

    private async Task ProcessPackage(string filename)
    {
        const string defaultAuthor = "RailSimulator";
        const Protection defaultProtection = Protection.Unprotected;

        var fileInfo = new FileInfo(filename);

        await using var filestream = fileInfo.OpenRead();

        var author = defaultAuthor;
        var eProtection = defaultProtection;
        var extension = Path.GetExtension(filename);

        var entryNameIndex = 0;

        if (extension == ".rwp")
        {
            var authorBytes = new byte[filestream.ReadByte()];
            await filestream.ReadExactlyAsync(authorBytes);
            eProtection = (Protection) filestream.ReadByte();
            author = Encoding.UTF8.GetString(authorBytes);
        }
        else
        {
            entryNameIndex = 1;
        }

        var package = new Package
        {
            Name = Path.GetFileNameWithoutExtension(fileInfo.Name),
            Author = author,
            Protection = eProtection,
            Assets = [],
        };

        await using var archiveStream = new OffsetReadStream(filestream, filestream.Position);
        using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read);

        RaisePackageInstallProgress(InstallStage.Scanning);

        var zeroByteEntryCount = archive.Entries.Count(entry => entry.Length == 0L);

        if (zeroByteEntryCount > 0)
        {
            Log.Information("installation for package {Package} encountered files with 0 bytes", filename);
        }

        RaisePackageInstallProgress(InstallStage.ClearingCache);

        await Task.Delay(200).ConfigureAwait(false);

        DeleteAllBlueprintsPak();

        var gameDirectory = Paths.GetGameDirectory();
        var entryCount = archive.Entries.Count;
        var reportedProgress = -1;

        for (var i = 0; i < entryCount; i++)
        {
            var entry = archive.Entries[i];
            var progress = (int) Math.Ceiling((double)(100 * i) / entryCount);

            if (progress != reportedProgress)
            {
                reportedProgress = progress;
                RaisePackageInstallProgress(progress, i + 1, entryCount);
            }

            if (entry.Length is 0)
            {
                continue;
            }

            var entryRelativePath = entry.FullName[entryNameIndex..].Replace('\\', '/').TrimStart('/');
            var entryFilename = Path.GetFileName(entryRelativePath);
            var entryGamePath = Paths.ResolveWithin(gameDirectory, entryRelativePath);

            var writtenPaths = entryFilename switch
            {
                "Scenarios.bin" => [],
                "Route.xml" => await ExtractRouteDotXml(entry, entryGamePath),
                "ScenarioInfo.xml" => await ExtractScenarioInfoDotXml(entry, entryGamePath),
                _ => ExtractRpkEntry(entry, entryGamePath),
            };

            foreach (var writtenPath in writtenPaths)
            {
                var assetKey = Path.GetRelativePath(gameDirectory, writtenPath).Replace('/', '\\');

                package.Assets.Add(assetKey);
            }
        }

        package.SavePackageInfo();

        RaisePackageInstallProgress(InstallStage.Installed, false);

        await Task.Delay(6000).ConfigureAwait(false);
    }

    private static async Task<byte[]> ReadEntryBytes(ZipArchiveEntry entry)
    {
        var bytes = new byte[entry.Length];

        await using var stream = entry.Open();
        await stream.ReadExactlyAsync(bytes);

        return bytes;
    }

    private static string GetPackedXmlText(byte[] bytes)
    {
        var index = 37 * bytes[0] + 2;

        return Encoding.UTF8.GetString(bytes, index, bytes.Length - index);
    }

    private static async Task<List<string>> ExtractRouteDotXml(ZipArchiveEntry entry, string entryGamePath)
    {
        var bytes = await ReadEntryBytes(entry);
        var routeXmlText = GetPackedXmlText(bytes);
        var routesDirectory = Path.GetDirectoryName(entryGamePath)
            ?? throw new Exception($"failed to get directory path for {entry.FullName}");

        return SplitPropertiesXml(
            routeXmlText,
            "cRouteProperties",
            guid => Path.Join(guid, "RouteProperties.xml"),
            routesDirectory);
    }

    private static async Task<List<string>> ExtractScenarioInfoDotXml(ZipArchiveEntry entry, string entryGamePath)
    {
        var bytes = await ReadEntryBytes(entry);
        var scenarioXmlText = GetPackedXmlText(bytes);
        var routeDirectory = Path.GetDirectoryName(entryGamePath)
            ?? throw new Exception($"failed to get directory path for {entry.FullName}");

        return SplitPropertiesXml(
            scenarioXmlText,
            "cScenarioProperties",
            guid => Path.Join("Scenarios", guid, "ScenarioProperties.xml"),
            routeDirectory);
    }

    private static List<string> SplitPropertiesXml(
        string xmlText,
        string elementName,
        Func<string, string> getRelativeOutputPath,
        string outputDirectory)
    {
        var results = new List<string>();
        var separator = $"\t\t</{elementName}>";

        foreach (var str in xmlText.Split(separator, StringSplitOptions.RemoveEmptyEntries))
        {
            if (str.Trim().Length == 0)
            {
                continue;
            }

            var xmlStr = str.Replace(
                $"<{elementName} d:id=",
                $"<{elementName} xmlns:d=\"http://www.kuju.com/TnT/2003/Delta\" d:version=\"1.0\" d:id="
            );

            var guid = ExtractGuid(xmlStr);
            var fileName = Paths.ResolveWithin(outputDirectory, getRelativeOutputPath(guid));
            var fileInfo = new FileInfo(fileName);

            Directory.CreateDirectory(fileInfo.DirectoryName!);

            if (fileInfo.Exists)
            {
                fileInfo.Attributes = FileAttributes.Normal;
            }

            using (var text = fileInfo.CreateText())
            {
                text.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                text.Write(xmlStr);
                text.WriteLine(separator);
            }

            results.Add(fileName);
        }

        return results;
    }

    private static List<string> ExtractRpkEntry(ZipArchiveEntry entry, string entryGamePath)
    {
        var isForbidden = ForbiddenExtensions.Contains(Path.GetExtension(entryGamePath));

        if (!isForbidden)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(entryGamePath)!);
            entry.ExtractToFile(entryGamePath, true);
        }

        return [entryGamePath];
    }

    private static string ExtractGuid(string value)
    {
        const string emptyValue = "00000000-0000-0000-0000-000000000000";

        var start = value.IndexOf("<DevString d:type=\"cDeltaString\">", StringComparison.Ordinal);
        var end = value.IndexOf("</DevString>", StringComparison.Ordinal);

        var a = start + "<DevString d:type=\"cDeltaString\">".Length;
        var b = end - start - "<DevString d:type=\"cDeltaString\">".Length;

        return start >= 0 && end >= 0 ? value.Substring(a, b) : emptyValue;
    }

    private static void DeleteAllBlueprintsPak()
    {
        var assets = Paths.GetAssetsDirectory();

        foreach (var provider in Directory.GetDirectories(assets))
        {
            foreach (var product in Directory.GetDirectories(provider))
            {
                var blueprintPaks = Directory
                    .EnumerateFiles(product, "*.pak", SearchOption.TopDirectoryOnly)
                    .Where(path => string.Equals(Path.GetFileName(path), "Blueprints.pak", StringComparison.OrdinalIgnoreCase));

                foreach (var target in blueprintPaks)
                {
                    try
                    {
                        File.Delete(target);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to remove file at {Path}", target);
                    }
                }
            }
        }
    }
}
