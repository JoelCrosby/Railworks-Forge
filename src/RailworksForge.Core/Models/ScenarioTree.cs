namespace RailworksForge.Core.Models;

public class ScenarioTree
{
    public List<ScenarioTreeNode> Nodes { get; init; } = new();
}

public class ScenarioTreeNode
{
    public string Name { get; init; }

    public string? Content { get; init; }

    public List<ScenarioTreeNode> ChildNodes { get; init; }
}
