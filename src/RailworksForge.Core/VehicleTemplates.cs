namespace RailworksForge.Core;

public class VehicleTemplates
{
    private static string EngineTemplate => GetTextResource("Engine.xml");
    private static string WagonTemplate => GetTextResource("Wagon.xml");
    private static string TenderTemplate => GetTextResource("Tender.xml");

    private static string GetTextResource(string name)
    {
        var assembly = typeof(VehicleTemplates).Assembly;
        var resource = assembly.GetManifestResourceStream($"RailworksForge.Core.Resources.{name}");

        if (resource is null)
        {
            throw new Exception($"could not resource in assembly with name {name}");
        }

        using var reader = new StreamReader(resource);

        return reader.ReadToEnd();
    }

    public static string GetXml(VehicleType type)
    {
        return type switch
        {
            VehicleType.Engine => EngineTemplate,
            VehicleType.Wagon => WagonTemplate,
            VehicleType.Tender => TenderTemplate,
            _ => throw new Exception("Unknown vehicle type."),
        };
    }
}
