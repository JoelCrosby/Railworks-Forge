namespace RailworksForge.Core.Scenarios;

public enum ScenarioChangeKind
{
    ReplaceConsists,
    DeleteConsists,
    AddVehicle,
    ReplaceVehicles,
    DeleteVehicle,
    MoveVehicle,
    FlipVehicle,
}

// The UI words each change itself, so Core only reports what changed and which service it touched.
public sealed record ScenarioChange(ScenarioChangeKind Kind, string ServiceName, int Count);
