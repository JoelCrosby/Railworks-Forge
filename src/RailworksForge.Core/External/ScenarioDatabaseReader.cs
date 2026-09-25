using System.Globalization;

using RailworksForge.Core.Models;

namespace RailworksForge.Core.External;

public static class ScenarioDatabaseReader
{
    public static Dictionary<string, ScenarioPlayerInfo> Read(byte[] data, CancellationToken cancellationToken = default)
    {
        var reader = new SerzReader(data);
        var scenarios = new Dictionary<string, ScenarioPlayerInfo>(StringComparer.OrdinalIgnoreCase);
        var depth = 0;
        var entry = default(ScenarioEntry);

        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (reader.Kind)
            {
                case SerzNodeKind.Open:
                    depth++;

                    if (entry is null && reader.Name == "sSDScenario")
                    {
                        entry = new ScenarioEntry { Depth = depth };
                    }
                    else if (entry is not null && reader.Name == "ScenarioID")
                    {
                        entry.ScenarioIdDepth = depth;
                    }

                    break;
                case SerzNodeKind.Close:
                    var closesScenarioId = entry?.ScenarioIdDepth == depth;
                    var closesScenario = entry?.Depth == depth;

                    if (closesScenarioId)
                    {
                        entry!.ScenarioIdDepth = null;
                    }

                    if (closesScenario)
                    {
                        AddScenario(scenarios, entry!);
                        entry = null;
                    }

                    depth--;
                    break;
                case SerzNodeKind.Value when entry is not null:
                    ReadScenarioValue(entry, reader);
                    break;
            }
        }

        return scenarios;
    }

    private static void ReadScenarioValue(ScenarioEntry entry, SerzReader reader)
    {
        var isInsideScenarioId = entry.ScenarioIdDepth is not null;

        switch (reader.Name)
        {
            case "DevString" when isInsideScenarioId:
                entry.ScenarioId ??= reader.Value;
                break;
            case "Score":
                entry.Score ??= ParseInteger(reader.Value);
                break;
            case "Completion":
                entry.Completion ??= reader.Value;
                break;
            case "MedalsAwarded":
                entry.MedalsAwarded ??= ParseInteger(reader.Value);
                break;
        }
    }

    private static void AddScenario(Dictionary<string, ScenarioPlayerInfo> scenarios, ScenarioEntry entry)
    {

        if (string.IsNullOrWhiteSpace(entry.ScenarioId))
        {
            return;
        }

        scenarios.TryAdd(entry.ScenarioId, new ScenarioPlayerInfo
        {
            ScenarioId = entry.ScenarioId,
            Score = entry.Score ?? 0,
            Completion = entry.Completion ?? string.Empty,
            MedalsAwarded = entry.MedalsAwarded ?? 0,
        });
    }

    private static int ParseInteger(string value)
    {
        var isInteger = int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result);

        return isInteger ? result : 0;
    }

    private sealed class ScenarioEntry
    {
        public required int Depth { get; init; }

        public int? ScenarioIdDepth { get; set; }

        public string? ScenarioId { get; set; }

        public int? Score { get; set; }

        public string? Completion { get; set; }

        public int? MedalsAwarded { get; set; }
    }
}
