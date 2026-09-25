using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;

using RailworksForge.Core.Config;
using RailworksForge.Core.External;
using RailworksForge.Core.Models;

using Serilog;

namespace RailworksForge.Core;

public class ScenarioDatabaseService : IDisposable
{
    // The game rewrites the database and its MD5 file in several steps, so wait for writes to settle.
    private static readonly TimeSpan ChangeSettleDelay = TimeSpan.FromSeconds(2);

    private readonly SemaphoreSlim _loadLock = new (1, 1);
    private readonly Subject<Unit> _databaseChanges = new ();

    private Dictionary<string, ScenarioPlayerInfo> _scenarios = new (StringComparer.OrdinalIgnoreCase);
    private bool _hasScenarios;
    private FileSystemWatcher? _watcher;
    private IDisposable? _changeSubscription;

    public event Action? Updated;

    public async Task LoadScenarioDatabase(CancellationToken cancellationToken = default)
    {
        await _loadLock.WaitAsync(cancellationToken);

        try
        {
            await LoadLatestScenarios(cancellationToken);
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public void WatchForChanges()
    {

        if (_watcher is not null)
        {
            return;
        }

        var databasePath = GetDatabasePath();

        _watcher = new FileSystemWatcher(Path.GetDirectoryName(databasePath)!, Path.GetFileName(databasePath))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
        };

        _watcher.Changed += (_, _) => _databaseChanges.OnNext(Unit.Default);
        _watcher.Created += (_, _) => _databaseChanges.OnNext(Unit.Default);
        _watcher.Renamed += (_, _) => _databaseChanges.OnNext(Unit.Default);

        _changeSubscription = _databaseChanges
            .Throttle(ChangeSettleDelay)
            .Select(_ => Observable.FromAsync(ReloadAfterChange))
            .Concat()
            .Subscribe();

        _watcher.EnableRaisingEvents = true;

        Log.Information("watching scenario database for changes @ {Path}", databasePath);
    }

    public ScenarioPlayerInfo GetScenario(string id)
    {
        return _scenarios.GetValueOrDefault(id) ?? ScenarioPlayerInfo.Empty;
    }

    public void Dispose()
    {
        _changeSubscription?.Dispose();
        _watcher?.Dispose();
        _databaseChanges.Dispose();
        _loadLock.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task LoadLatestScenarios(CancellationToken cancellationToken)
    {
        var databasePath = GetDatabasePath();

        if (!Paths.Exists(databasePath))
        {
            throw new Exception($"failed to get find scenario database in expected path: {databasePath}");
        }

        var fingerprint = GetFingerprint(databasePath);

        if (!_hasScenarios && ReadCache() is {} cache)
        {
            SetScenarios(cache.Scenarios);

            if (cache.Fingerprint == fingerprint)
            {
                Log.Information("using cached scenario database");

                return;
            }
        }

        var sw = Stopwatch.StartNew();
        var data = await File.ReadAllBytesAsync(databasePath, cancellationToken);
        var scenarios = await Task.Run(() => ScenarioDatabaseReader.Read(data, cancellationToken), cancellationToken);

        Log.Information("read {Count} scenarios from scenario database in {Elapsed}ms", scenarios.Count, sw.ElapsedMilliseconds);

        SetScenarios(scenarios);

        var updatedCache = new ScenarioDatabaseCache
        {
            Fingerprint = fingerprint,
            Scenarios = scenarios,
        };

        await WriteCache(updatedCache, cancellationToken);
    }

    private async Task ReloadAfterChange()
    {
        try
        {
            Log.Information("scenario database changed, reloading");
            await LoadScenarioDatabase();
        }
        catch (Exception e)
        {
            Log.Warning(e, "failed to reload changed scenario database");
        }
    }

    private void SetScenarios(Dictionary<string, ScenarioPlayerInfo> scenarios)
    {
        _scenarios = new Dictionary<string, ScenarioPlayerInfo>(scenarios, StringComparer.OrdinalIgnoreCase);
        _hasScenarios = true;

        Updated?.Invoke();
    }

    // Size and modified time catch almost every rewrite; the game's own MD5 also catches a restored file with an old timestamp.
    private static string GetFingerprint(string databasePath)
    {
        var info = new FileInfo(databasePath);
        var hashPath = databasePath + ".MD5";
        var hash = File.Exists(hashPath) ? Convert.ToHexString(File.ReadAllBytes(hashPath)) : string.Empty;

        return $"{hash}:{info.Length}:{info.LastWriteTimeUtc.Ticks}";
    }

    private static ScenarioDatabaseCache? ReadCache()
    {
        var cachePath = GetCachePath();

        if (!File.Exists(cachePath))
        {
            return null;
        }

        try
        {
            using var json = File.OpenRead(cachePath);

            return JsonSerializer.Deserialize(json, SourceGenerationContext.Default.ScenarioDatabaseCache);
        }
        catch (JsonException e)
        {
            Log.Warning(e, "ignoring unreadable scenario database cache @ {Path}", cachePath);

            return null;
        }
    }

    private static async Task WriteCache(ScenarioDatabaseCache cache, CancellationToken cancellationToken)
    {
        var cachePath = GetCachePath();
        var temporaryPath = cachePath + ".tmp";

        await using (var output = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(output, cache, SourceGenerationContext.Default.ScenarioDatabaseCache, cancellationToken);
        }

        File.Move(temporaryPath, cachePath, true);
    }

    private static string GetDatabasePath()
    {
        return Path.Join(Paths.GetGameDirectory(), "Content", "SDBCache.bin");
    }

    private static string GetCachePath()
    {
        Directory.CreateDirectory(Paths.GetCacheFolder());

        return Path.Join(Paths.GetCacheFolder(), "ScenarioDatabase.json");
    }
}
