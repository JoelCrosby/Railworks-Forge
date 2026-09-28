using System.Collections.Concurrent;

using RailworksForge.Core.Models;

using Serilog;

namespace RailworksForge.Core;

public sealed record ArchiveIndex(DateTime LastWriteTimeUtc, HashSet<string> Entries);

public class Cache
{
    public static readonly ConcurrentDictionary<string, AcquisitionState?> ConsistAcquisitionStates = new ();

    public static readonly ConcurrentDictionary<string, AcquisitionState?> BlueprintAcquisitionStates = new ();

    public static readonly ConcurrentDictionary<string, ArchiveIndex> ArchiveCache = new();

    public static readonly ConcurrentDictionary<string, List<string>> ProductArchives = new(StringComparer.OrdinalIgnoreCase);

    public static void ClearAcquisitionStates()
    {
        BlueprintAcquisitionStates.Clear();
        ConsistAcquisitionStates.Clear();
        ProductArchives.Clear();
    }

    public static void ClearAssetCaches()
    {
        ClearAcquisitionStates();
        ArchiveCache.Clear();
    }

    public static void ClearScenarioCache(Scenario scenario)
    {
        try
        {
            File.Delete(scenario.CachedDocumentPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "failed to delete cached scenario document");
        }

        var directory = Paths.GetContentDirectory();

        var files = new []
        {
            "RVDBCache.bin",
            "RVDBCache.bin.MD5",
            "SDBCache.bin",
            "SDBCache.bin.MD5",
            "TMCache.dat",
            "TMCache.dat.MD5",
        };

        foreach (var file in files)
        {
            TryDeleteFile(directory, file);
        }

        return;

        static void TryDeleteFile(string dir, string filename)
        {
            var path = Path.Join(dir, filename);

            try
            {
                File.Delete(path);
            }
            catch (Exception e)
            {
                Log.Warning(e, "Failed to delete game cache file {Path}", path);
            }
        }
    }
}
