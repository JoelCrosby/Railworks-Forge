namespace RailworksForge.Core.Models;

public class Consist
{
    public required string Id { get; init; }

    public required string LocomotiveName { get; init; }

    public string? LocoAuthor { get; init; }

    public LocoClass? LocoClass { get; set; }

    public required string ServiceName { get; init; }

    public bool PlayerDriver { get; init; }

    public required string BlueprintId { get; init; }

    public required string ServiceId { get; init; }

    public string? RawText { get; init; }
}

public class ConsistRailVehicle
{
    public required string Id { get; init; }

    public required string LocomotiveName { get; init; }

    public string? UniqueNumber { get; init; }

    public bool Flipped { get; init; }

    public required string BlueprintId { get; init; }

    public required string BlueprintSetIdProvider { get; init; }

    public required string BlueprintSetIdProduct { get; init; }

    public AcquisitionState AcquisitionState => GetAcquisitionState();

    private string ProductDirectory => Path.Join(
        Paths.GetAssetsDirectory(),
        BlueprintSetIdProvider,
        BlueprintSetIdProduct
    );

    private AcquisitionState GetAcquisitionState()
    {
        var agnosticBlueprintIdPath = BlueprintId.Replace('\\', Path.DirectorySeparatorChar);
        var binaryPath = agnosticBlueprintIdPath.Replace(".xml", ".bin");
        var blueprintPath = Path.Join(ProductDirectory, binaryPath);

        if (File.Exists(blueprintPath))
        {
            return AcquisitionState.Found;
        }

        if (!Directory.Exists(ProductDirectory))
        {
            return AcquisitionState.Missing;
        }

        var archives = Directory.EnumerateFiles(ProductDirectory, "*.ap", SearchOption.AllDirectories);

        foreach (var archive in archives)
        {
            var found = Archives.EntryExists(archive, binaryPath);

            if (found)
            {
                return AcquisitionState.Found;
            }
        }

        return AcquisitionState.Missing;
    }
}

public class ConsistBlueprint
{
    public required string LocomotiveName { get; init; }

    public required string DisplayName { get; init; }
}

public enum AcquisitionState
{
    Unknown = 0,
    Found = 1,
    Missing = 2,
}
