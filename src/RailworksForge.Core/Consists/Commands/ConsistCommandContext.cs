using AngleSharp.Dom;

using RailworksForge.Core.Scenarios;

namespace RailworksForge.Core.Consists.Commands;

public record ConsistCommandContext
{
    public required Scenario Scenario { get; init; }

    public required IDocument ScenarioDocument { get; init; }

    public required IDocument ScenarioPropertiesDocument { get; init; }
}
