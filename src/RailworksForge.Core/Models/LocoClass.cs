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
            "LOCO_CLASS_DIESEL" => LocoClass.Diesel,
            "LOCO_CLASS_ELECTRIC" => LocoClass.Electric,
            "LOCO_CLASS_STEAM" => LocoClass.Steam,
            _ => LocoClass.Unknown,
        };
    }
}
