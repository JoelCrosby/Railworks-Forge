using System.IO.Compression;
using System.Text;

using Serilog;

namespace RailworksForge.Core.Packaging;

public class Packager
{
    private readonly HashSet<string> _forbiddenExtensions =
    [
        ".bak",
        ".pak",
        ".tgt",
        ".cost",
    ];

    private void InstallPackage(string filename)
    {
        var str = Path.Join("PackageInfo", Path.GetFileNameWithoutExtension(filename) + ".pi");
        var installed = new FileInfo(str).Exists;

        if (installed) return;

        ProcessPackage(filename);
    }

    private void ProcessPackage(string filename)
    {
        const string defaultAuthor = "RailSimulator";
        const Protection defaultProtection = Protection.Unprotected;

        var fileInfo = new FileInfo(filename);
        var file = fileInfo.OpenRead();
        var author = defaultAuthor;
        var eProtection = defaultProtection;

        var entryNameIndex = 0;

        if (Path.GetExtension(filename) == ".rwp")
        {
            var authorBytes = new byte[file.ReadByte()];
            _ = file.Read(authorBytes, 0, authorBytes.Length);
            eProtection = (Protection)file.ReadByte();
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

        var zipFile = ZipFile.OpenRead(filename);
        var zeroByteErrors = new List<string>();

        foreach (var entry in zipFile.Entries)
        {
            var key = entry.Name[entryNameIndex..];

            if (entry.Length == 0L) zeroByteErrors.Add(key);
        }

        zipFile.Dispose();
        file.Close();

        if (zeroByteErrors.Count > 0)
            Log.Information("installation for package {Package} encountered files with 0 bytes", filename);

        var baseInputStream = fileInfo.OpenRead();

        if (Path.GetExtension(filename) == ".rwp")
        {
            var head = 1 + baseInputStream.ReadByte();

            while (head-- > 0)
            {
                baseInputStream.ReadByte();
            }
        }

        // DeleteAllBlueprintsPak(false);

        var rpkStream = new ZipArchive(baseInputStream);

        foreach (var entry in rpkStream.Entries)
        {
            var entryName = entry.Name[entryNameIndex..];
            var entryFilename = Path.GetFileName(entryName);
            var assets = new List<string>();

            if (entryFilename != "Scenarios.bin")
            {
                if (entryFilename == "Route.xml")
                {
                    assets = ExtractRouteDotXml(entry, entryNameIndex);
                }
                else if (Path.GetFileName(entryName) == "ScenarioInfo.xml")
                {
                    assets = ExtractScenarioInfoDotXml(entry, entryNameIndex);
                }
                else
                {
                    if (_forbiddenExtensions.Contains(Path.GetExtension(entryName)) == false)
                    {
                        ExtractRpkEntry(entry, entryName);
                    }

                    assets = [entryName];
                }
            }

            foreach (var key in assets) package.Assets[key] = null;
        }

        rpkStream.Dispose();
        baseInputStream.Close();
    }

    private static List<string> ExtractRouteDotXml(ZipArchiveEntry rpkEntry, int pathOffset)
    {
        var numArray = new byte[rpkEntry.Length];
        _ = rpkEntry.Open().Read(numArray, 0, numArray.Length);
        var index = 37 * numArray[0] + 2;

        var routeXmlText = Encoding.UTF8.GetString(numArray, index, (int)rpkEntry.Length - index);
        var directoryPath = Path.GetDirectoryName(rpkEntry.Name[pathOffset..]);

        if (directoryPath is null) throw new Exception($"failed to get directory path for {rpkEntry.Name}");

        var directoryInfo = new DirectoryInfo(directoryPath);

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
            var fileName = intoRoutesDI + "\\" + guid + "\\RouteProperties.xml";
            var fileInfo = new FileInfo(fileName);

            var dirName = Path.GetDirectoryName(fileInfo.FullName);

            if (dirName is null)
            {
                throw new Exception($"failed to get directory path for {fileInfo.FullName}");
            }

            Directory.CreateDirectory(dirName);

            if (fileInfo.Exists) fileInfo.Attributes = FileAttributes.Normal;

            using (TextWriter text = fileInfo.CreateText())
            {
                text.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                text.Write(xmlStr);
                text.WriteLine(separator[0]);
                text.Close();
            }

            if (fileName[0] == '\\') fileName = fileName[1..];

            result.Add(fileName);
        }

        return result;
    }

    private static List<string> ExtractScenarioInfoDotXml(ZipArchiveEntry rpkEntry, int pathOffset)
    {
        var numArray = new byte[rpkEntry.Length];
        _ = rpkEntry.Open().Read(numArray, 0, (int)rpkEntry.Length);
        var index = 37 * numArray[0] + 2;

        var routeXmlText = Encoding.UTF8.GetString(numArray, index, (int)rpkEntry.Length - index);
        var directoryPath = Path.GetDirectoryName(rpkEntry.Name[pathOffset..]);

        if (directoryPath is null) throw new Exception($"failed to get directory path for {rpkEntry.Name}");

        var directoryInfo = new DirectoryInfo(directoryPath);

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
            if (str.Trim().Length == 0) continue;

            var xmlStr = str.Replace(
                "<cScenarioProperties d:id=",
                "<cScenarioProperties xmlns:d=\"http://www.kuju.com/TnT/2003/Delta\" d:version=\"1.0\" d:id="
            );

            var guid = ExtractGuid(xmlStr);
            var fileName = $@"{intoRouteDi}\Scenarios\{guid}\ScenarioProperties.xml";
            var fileInfo = new FileInfo(fileName);

            var dirName = Path.GetDirectoryName(fileInfo.FullName);

            if (dirName is null)
            {
                throw new Exception($"failed to get directory path for {fileInfo.FullName}");
            }

            Directory.CreateDirectory(dirName);

            if (fileInfo.Exists) fileInfo.Attributes = FileAttributes.Normal;

            using (TextWriter text = fileInfo.CreateText())
            {
                text.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                text.Write(xmlStr);
                text.WriteLine(separator[0]);
                text.Close();
            }

            if (fileName[0] == '\\') fileName = fileName[1..];

            results.Add(fileName);
        }

        return results;
    }

    private static void ExtractRpkEntry(ZipArchiveEntry entry, string filename)
    {
        var directoryName = Path.GetDirectoryName(filename);

        if (directoryName is null)
        {
            throw new Exception($"failed to get directory path for {filename}");
        }

        if (directoryName.Length == 0)
        {
            return;
        }

        Directory.CreateDirectory(directoryName);

        entry.ExtractToFile(filename, true);
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
}
