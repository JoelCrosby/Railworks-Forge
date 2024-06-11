namespace RailworksForge.Core.Models;

public record RailVehicle
{
    public required string LocoName { get; init; }

    public required string DisplayName { get; init; }

    public virtual bool Equals(RailVehicle? other)
    {
        if (other is null) return false;

        return LocoName == other.LocoName;
    }

    public override int GetHashCode()
    {
        return LocoName.GetHashCode();
    }
}
