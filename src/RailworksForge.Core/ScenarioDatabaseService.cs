using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Subjects;
using System.Text.Json;

using AngleSharp.Common;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.External;
using RailworksForge.Core.Models;

using Serilog;

namespace RailworksForge.Core;

public static class ScenarioDatabaseService
{
    private static ConcurrentDictionary<string, ScenarioPlayerInfo>? _scenarioDictionary;

    public static readonly BehaviorSubject<bool> IsLoaded = new (false);

    private static async Task ParseDatabase(CancellationToken cancellationToken)
    {
        if (GetJsonCache() is {} jsonCache)
        {
            Log.Information("using cached scenario database");

            _scenarioDictionary = jsonCache;
        }
        else
        {
            _scenarioDictionary = await GetScenarioDictionary(cancellationToken);
        }

        IsLoaded.OnNext(true);
    }

    private static readonly ScenarioPlayerInfo Empty = new ();

    private static async Task<ConcurrentDictionary<string, ScenarioPlayerInfo>> GetScenarioDictionary(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        var path = Path.Join(Paths.GetGameDirectory(), "Content", "SDBCache.bin");

        if (!Paths.Exists(path))
        {
            throw new Exception($"failed to get find scenario database in expected path: {path}");
        }

        var serialised = await Serz.Convert(path, cancellationToken);
        var file = File.OpenRead(serialised.OutputPath);

        var document = await XmlParser.ParseDocumentAsync(file, cancellationToken);

        Log.Information("parsed scenario database document in {Elapsed}ms", sw.ElapsedMilliseconds);

        sw.Restart();

        var scenarioDictionary = new ConcurrentDictionary<string, ScenarioPlayerInfo>();

        foreach (var element in document.QuerySelectorAll("sSDScenario"))
        {
            var id = element.SelectTextContent("ScenarioID DevString");

            if (string.IsNullOrWhiteSpace(id)) continue;

            var score = element.SelectInteger("Score");
            var completion = element.SelectTextContent("Completion");
            var medalsAwarded = element.SelectInteger("MedalsAwarded");

            scenarioDictionary.TryAdd(id, new ScenarioPlayerInfo
            {
                ScenarioId = id,
                Score = score,
                Completion = completion,
                MedalsAwarded = medalsAwarded,
            });
        }

        Log.Information("built scenario cache in {Elapsed}ms", sw.ElapsedMilliseconds);

        var cachePath = GetCachePath();
        var jsonCache = JsonSerializer.Serialize(scenarioDictionary);

        await File.WriteAllTextAsync(cachePath, jsonCache, cancellationToken);

        return scenarioDictionary;
    }

    private static ConcurrentDictionary<string, ScenarioPlayerInfo>? GetJsonCache()
    {
        var cachePath = GetCachePath();

        if (!Paths.Exists(cachePath)) return null;

        Log.Information("found cached scenario database @ {Path}", cachePath);

        var jsonText = File.ReadAllText(cachePath);
        return JsonSerializer.Deserialize<ConcurrentDictionary<string, ScenarioPlayerInfo>>(jsonText);
    }

    private static string GetCachePath()
    {
        return Path.Join(Paths.GetCacheFolder(), "SDBCache.json");
    }

    public static async Task LoadScenarioDatabase()
    {
        await ParseDatabase(CancellationToken.None);
    }

    public static ScenarioPlayerInfo GetScenario(string id)
    {
        return _scenarioDictionary?.GetOrDefault(id, null) ?? Empty;
    }
}
