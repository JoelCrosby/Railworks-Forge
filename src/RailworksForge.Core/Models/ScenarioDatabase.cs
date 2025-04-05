namespace RailworksForge.Core.Models;

public class ScenarioPlayerInfo
{
    public required string ScenarioId { get; init; }

    public required int Score { get; init; }

    public required string Completion { get; init; }

    public required int MedalsAwarded { get; init; }

    public static readonly ScenarioPlayerInfo Empty = new ()
    {
        ScenarioId = string.Empty,
        Score = 0,
        Completion = string.Empty,
        MedalsAwarded = 0,
    };
}
