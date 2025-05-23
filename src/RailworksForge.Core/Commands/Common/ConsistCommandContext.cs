using AngleSharp.Dom;

using RailworksForge.Core.Models;

namespace RailworksForge.Core.Commands.Common;

public record ConsistCommandContext
{
    public required Scenario Scenario { get; init; }

    public required IDocument ScenarioDocument { get; init; }

    public required IDocument ScenarioPropertiesDocument { get; init; }
}
