using System.IO.Compression;

using AngleSharp.Html.Parser;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models;

namespace RailworksForge.Core;

public class RouteService
{
    public List<Route> GetRoutes()
    {
        var baseDir = Paths.GetScenariosDirectory();
        var routeFiles = Directory
            .EnumerateFiles(baseDir, "*",  SearchOption.AllDirectories)
            .Where(file => file.EndsWith("RouteProperties.xml") || file.EndsWith("MainContent.ap"))
            .ToList();

        return ReadRouteFiles(routeFiles);
    }

    private static List<Route> ReadRouteFiles(List<string> routeFiles)
    {
        var results = new HashSet<Route>();

        foreach (var path in routeFiles)
        {
            var route = Path.GetExtension(path) switch
            {
                ".xml" => ReadRouteFile(path),
                ".ap" => ReadCompressedRouteFile(path),
                _ => throw new Exception("unrecognised route extension"),
            };

            results.Add(route);
        }

        return results.OrderBy(route => route.Name).ToList();
    }

    private static Route ReadRouteFile(string path)
    {
        var file = File.ReadAllText(path);

        return ReadRouteProperties(path, file);
    }

    private static Route ReadCompressedRouteFile(string path)
    {
        using var archive = ZipFile.Open(path, ZipArchiveMode.Read);

        var properties = archive.Entries.FirstOrDefault(entry => entry.Name == "RouteProperties.xml");

        if (properties is null)
        {
            throw new Exception("failed to find route properties file in compressed archive");
        }

        var content = properties.Open();

        using var reader = new StreamReader(content);

        var file = reader.ReadToEnd();

        return ReadRouteProperties(path, file);
    }

    private static Route ReadRouteProperties(string path, string fileContent)
    {
        var id = Directory.GetParent(path)?.Name ?? string.Empty;
        var doc = new HtmlParser().ParseDocument(fileContent);
        var name = doc.SelectTextContnet("DisplayName English");
        var directoryPath = Directory.GetParent(path)?.FullName ?? string.Empty;

        return new Route
        {
            Id = id,
            Name = name,
            Path = directoryPath,
            RootPath = path,
            PackagingType = path.EndsWith(".xml") ? PackagingType.Unpacked : PackagingType.Packed,
        };
    }
}
