namespace RailworksForge.Core.Models;

public enum LocoClass
{
    Unknown,
    Diesel,
    Electric,
    Steam,
}

public static class LocoClassUtils
{
    public static LocoClass Parse(string locoClass)
    {
        return locoClass switch
        {
            "Diesel" => LocoClass.Diesel,
            "LOCO_CLASS_DIESEL" => LocoClass.Diesel,
            "Electric" => LocoClass.Electric,
            "LOCO_CLASS_ELECTRIC" => LocoClass.Electric,
            "Steam" => LocoClass.Steam,
            "LOCO_CLASS_STEAM" => LocoClass.Steam,
            _ => LocoClass.Unknown,
        };
    }
}
