using System.IO.Compression;
using System.Reactive.Subjects;
using System.Text;

using Serilog;

namespace RailworksForge.Core.Packaging;

public class Packager
{
    public Subject<InstallProgress> PackageInstallProgressSubject { get; } = new();

    private string _currentTask = string.Empty;

    private static readonly HashSet<string> ForbiddenExtensions =
    [
        ".bak",
        ".pak",
        ".tgt",
        ".cost",
    ];

    public async Task InstallPackage(string filename)
    {
        var packageName = Path.GetFileNameWithoutExtension(filename);

        RaisePackageInstallProgress($"Installing {packageName}");

        await Task.Delay(200).ConfigureAwait(false);

        var packageInfo = Path.Join(Paths.GetGameDirectory(), "PackageInfo", $"{packageName}.pi");
        var installed = Paths.Exists(packageInfo);

        if (installed)
        {
            RaisePackageInstallProgress($"Package {packageName} is already installed, aborting.");

            await Task.Delay(2000).ConfigureAwait(false);

            Log.Information("package {Package} already installed", packageName);
            return;
        }

        await ProcessPackage(filename);
    }

    private void RaisePackageInstallProgress(int progress, string? message = null)
    {
        var args = new InstallProgress
        {
            Progress = progress,
            Message = message ?? string.Empty,
            CurrentTask = _currentTask,
            IsLoading = true,
        };

        PackageInstallProgressSubject.OnNext(args);
    }

    private void RaisePackageInstallProgress(string currentTask, bool isLoading = true)
    {
        _currentTask = currentTask;

        var args = new InstallProgress
        {
            Progress = 0,
            Message = string.Empty,
            CurrentTask = _currentTask,
            IsLoading = isLoading,
        };

        PackageInstallProgressSubject.OnNext(args);
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
            _ = await filestream.ReadAsync(authorBytes);
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


        using var archiveStream = new MemoryStream();
        await filestream.CopyToAsync(archiveStream);

        using var archive = new ZipArchive(archiveStream);
        var zeroByteErrors = new List<string>();

        RaisePackageInstallProgress("Scanning package files...");

        foreach (var entry in archive.Entries)
        {
            var key = entry.Name[entryNameIndex..];

            if (entry.Length == 0L)
            {
                zeroByteErrors.Add(key);
            }
        }

        if (zeroByteErrors.Count > 0)
        {
            Log.Information("installation for package {Package} encountered files with 0 bytes", filename);
        }

        RaisePackageInstallProgress("Clearing blueprint .pak cache files...");

        await Task.Delay(200).ConfigureAwait(false);

        DeleteAllBlueprintsPak();

        var entryCount = archive.Entries.Count;

        for (var i = 0; i < entryCount; i++)
        {
            var entry = archive.Entries[i];
            var progress = (int) Math.Ceiling((double)(100 * i) / entryCount);

            RaisePackageInstallProgress(progress, $"Processing File {i + 1} of {entryCount}");

            var entryArchivePath = entry.FullName[entryNameIndex..].Replace('\\', Path.DirectorySeparatorChar);
            var entryFilename = Path.GetFileName(entryArchivePath);
            var assets = new List<string>();

            if (entry.Length == 0)
            {
                continue;
            }

            if (entryFilename != "Scenarios.bin")
            {
                if (entryFilename == "Route.xml")
                {
                    assets = await ExtractRouteDotXml(entry, entryNameIndex);
                }
                else if (Path.GetFileName(entryArchivePath) == "ScenarioInfo.xml")
                {
                    assets = await ExtractScenarioInfoDotXml(entry, entryNameIndex);
                }
                else
                {
                    if (ForbiddenExtensions.Contains(Path.GetExtension(entryArchivePath)) == false)
                    {
                        ExtractRpkEntry(entry, entryArchivePath);
                    }

                    assets = [entryArchivePath];
                }
            }

            foreach (var key in assets)
            {
                package.Assets[key] = null;
            }
        }

        RaisePackageInstallProgress($"Successfully Installed package {package.Name}", false);

        await Task.Delay(6000).ConfigureAwait(false);
    }

    private static async Task<List<string>> ExtractRouteDotXml(ZipArchiveEntry rpkEntry, int pathOffset)
    {
        var numArray = new byte[rpkEntry.Length];
        _ = await rpkEntry.Open().ReadAsync(numArray);
        var index = 37 * numArray[0] + 2;

        var routeXmlText = Encoding.UTF8.GetString(numArray, index, (int)rpkEntry.Length - index);
        var directoryPath = Path.GetDirectoryName(rpkEntry.Name[pathOffset..]);
        var gameDirectoryPath = Path.Join(Paths.GetGameDirectory(), directoryPath);

        if (gameDirectoryPath is null)
        {
            throw new Exception($"failed to get directory path for {rpkEntry.Name}");
        }

        var directoryInfo = new DirectoryInfo(gameDirectoryPath);

        return SplitRouteXml(routeXmlText, directoryInfo);
    }

    private static List<string> SplitRouteXml(string routeXmlText, DirectoryInfo intoRoutesDI)
    {
        var separator = new[]
        {
            "\t\t</cRouteProperties>",
        };

        var strArray = routeXmlText.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        var result = new List<string>();

        foreach (var str in strArray)
        {
            if (str.Trim().Length == 0) continue;

            var xmlStr = str.Replace(
                "<cRouteProperties d:id=",
                "<cRouteProperties xmlns:d=\"http://www.kuju.com/TnT/2003/Delta\" d:version=\"1.0\" d:id="
            );

            var guid = ExtractGuid(xmlStr);
            var fileName = Path.Join(intoRoutesDI.FullName, guid, "RouteProperties.xml");
            var fileInfo = new FileInfo(fileName);

            var dirName = Path.GetDirectoryName(fileInfo.FullName);

            if (dirName is null)
            {
                throw new Exception($"failed to get directory path for {fileInfo.FullName}");
            }

            Directory.CreateDirectory(dirName);

            if (fileInfo.Exists)
            {
                fileInfo.Attributes = FileAttributes.Normal;
            }

            using (TextWriter text = fileInfo.CreateText())
            {
                text.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                text.Write(xmlStr);
                text.WriteLine(separator[0]);
                text.Close();
            }

            if (fileName[0] == '\\')
            {
                fileName = fileName[1..];
            }

            result.Add(fileName);
        }

        return result;
    }

    private static async Task<List<string>> ExtractScenarioInfoDotXml(ZipArchiveEntry rpkEntry, int pathOffset)
    {
        var numArray = new byte[rpkEntry.Length];
        _ = await rpkEntry.Open().ReadAsync(numArray);
        var index = 37 * numArray[0] + 2;

        var routeXmlText = Encoding.UTF8.GetString(numArray, index, (int)rpkEntry.Length - index);
        var directoryPath = Path.GetDirectoryName(rpkEntry.Name[pathOffset..]);
        var gameDirectoryPath = Path.Join(Paths.GetGameDirectory(), directoryPath);

        if (directoryPath is null)
        {
            throw new Exception($"failed to get directory path for {rpkEntry.Name}");
        }

        var directoryInfo = new DirectoryInfo(gameDirectoryPath);

        return SplitScenariosXml(routeXmlText, directoryInfo);
    }

    private static List<string> SplitScenariosXml(string scenarioXml, DirectoryInfo intoRouteDi)
    {
        var results = new List<string>();

        var separator = new[]
        {
            "\t\t</cScenarioProperties>",
        };

        foreach (var str in scenarioXml.Split(separator, StringSplitOptions.RemoveEmptyEntries))
        {
            if (str.Trim().Length == 0)
            {
                continue;
            }

            var xmlStr = str.Replace(
                "<cScenarioProperties d:id=",
                "<cScenarioProperties xmlns:d=\"http://www.kuju.com/TnT/2003/Delta\" d:version=\"1.0\" d:id="
            );

            var guid = ExtractGuid(xmlStr);
            var fileName = Path.Join(intoRouteDi.FullName, "Scenarios", guid, "ScenarioProperties.xml");
            var fileInfo = new FileInfo(fileName);

            var dirName = Path.GetDirectoryName(fileInfo.FullName);

            if (dirName is null)
            {
                throw new Exception($"failed to get directory path for {fileInfo.FullName}");
            }

            Directory.CreateDirectory(dirName);

            if (fileInfo.Exists)
            {
                fileInfo.Attributes = FileAttributes.Normal;
            }

            using (TextWriter text = fileInfo.CreateText())
            {
                text.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                text.Write(xmlStr);
                text.WriteLine(separator[0]);
                text.Close();
            }

            if (fileName[0] == '\\')
            {
                fileName = fileName[1..];
            }

            results.Add(fileName);
        }

        return results;
    }

    private static void ExtractRpkEntry(ZipArchiveEntry entry, string entryPath)
    {
        var directory = Path.GetDirectoryName(entryPath);
        var filename = Path.GetFileName(entryPath);

        if (directory is null)
        {
            throw new Exception($"failed to get directory path for {entryPath}");
        }

        var destination = Path.Join(Paths.GetGameDirectory(), directory);
        var destinationFilename = Path.Join(destination, filename);

        Directory.CreateDirectory(destination);

        entry.ExtractToFile(destinationFilename, true);
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
                var target = Path.Join(product, "Blueprints.pak");

                if (Paths.Exists(target) is false)
                {
                    continue;
                }

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
