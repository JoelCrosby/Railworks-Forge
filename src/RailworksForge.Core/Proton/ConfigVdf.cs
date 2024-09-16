namespace RailworksForge.Core.Proton;

internal class ConfigVdf
{
    public InstallConfigStore? InstallConfigStore { get; set; }= new();
}

internal class InstallConfigStore
{
    public Software? Software { get; set; }= new();
}

internal class Software
{
    public Valve? Valve { get; set; }= new();
}

internal class Valve
{
    public Steam? Steam { get; set; }= new();
}

internal class Steam
{
    public Dictionary<string, CompatToolMapping>? CompatToolMapping { get; set; } = new();
}

internal class CompatToolMapping
{
    // ReSharper disable once InconsistentNaming
    public string? name { get; set; }

    // ReSharper disable once InconsistentNaming
    public string? config { get; set; }

    // ReSharper disable once InconsistentNaming
    public string? priority { get; set; }
}
