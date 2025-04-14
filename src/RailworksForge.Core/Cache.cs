using System.Collections.Concurrent;
using System.Diagnostics;

using Avalonia.Media.Imaging;

using RailworksForge.Core.Models;

using Serilog;

namespace RailworksForge.Core;

public class Cache
{
    public static ConcurrentDictionary<string, AcquisitionState> AcquisitionStates = new ();

    public static ConcurrentDictionary<string, HashSet<string>> ArchiveFileCache = new ();

    public static readonly ConcurrentDictionary<string, HashSet<string>> ArchiveCache = new();

    public static readonly ConcurrentDictionary<(string, string), Bitmap?> ImageCache = new();

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

        var directory = Paths.GetRoutesDirectory();

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
                Debug.WriteLine("failed to delete file at path {path}", e.Message);
            }
        }
    }
}
