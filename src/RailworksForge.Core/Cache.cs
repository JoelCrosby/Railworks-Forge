using System.Collections.Concurrent;

using RailworksForge.Core.Models;

namespace RailworksForge.Core;

public class Cache
{
    public static ConcurrentDictionary<string, AcquisitionState> AcquisitionStates = new ();

    public static ConcurrentDictionary<string, HashSet<string>> ArchiveFileCache = new ();
}
