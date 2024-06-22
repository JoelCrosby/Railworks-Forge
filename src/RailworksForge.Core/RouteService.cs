using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models;

namespace RailworksForge.Core;

public class RouteService
{
    public static List<Route> GetRoutes()
    {
        var baseDir = Paths.GetRoutesDirectory();
        var routeFiles = Directory
            .EnumerateFiles(baseDir, "*",  SearchOption.AllDirectories)
            .Where(file => file.EndsWith("RouteProperties.xml") || file.EndsWith("MainContent.ap"))
            .Aggregate(new Dictionary<string, List<string>>(), (acc, route) =>
            {
                var dir = Path.GetDirectoryName(route);

                if (dir is null)
                {
                    throw new Exception($"could not get dirname for route path {route}");
                }

                if (acc.TryGetValue(dir, out var value))
                {
                    value.Add(route);
                }
                else
                {
                    acc.Add(dir, [route]);
                }

                return acc;
            })
            .Select(pair =>
            {
                if (pair.Value.FirstOrDefault(p => p.EndsWith(".xml")) is {} xml)
                {
                    return xml;
                }

                return pair.Value.First();
            })
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
        var file = Archives.GetTextFileContentFromPath(path, "/RouteProperties.xml");

        return ReadRouteProperties(path, file);
    }

    private static Route ReadRouteProperties(string path, string fileContent)
    {
        var id = Directory.GetParent(path)?.Name ?? string.Empty;
        var doc = XmlParser.ParseDocument(fileContent);
        var name = doc.SelectTextContent("DisplayName English");
        var directoryPath = Directory.GetParent(path)?.FullName ?? string.Empty;

        return new Route
        {
            Id = id,
            Name = name,
            RoutePropertiesPath = path,
            DirectoryPath = directoryPath,
            PackagingType = path.EndsWith(".xml") ? PackagingType.Unpacked : PackagingType.Packed,
        };
    }
}
