namespace RailworksForge.Core.Extensions;

public static class IEnumerableExtensions
{
    public static List<DirectoryInfo> ToDirectoryInfoList(this IEnumerable<string> paths)
    {
        return paths.Select(p => new DirectoryInfo(p)).OrderBy(p => p.Name).ToList();
    }
}
