using AngleSharp.Dom;

namespace RailworksForge.Core.Consists.Commands;

internal static class ConsistVehicleElements
{
    public static IElement FindServiceConsist(IDocument document, Consist consist)
    {
        var serviceConsist = Consist.GetServiceConsist(document, consist);

        if (serviceConsist is null)
        {
            throw new Exception("unable to find scenario consist");
        }

        return serviceConsist;
    }

    public static IElement FindVehicle(IElement serviceConsist, ConsistRailVehicle vehicle)
    {
        var railVehicle = serviceConsist
            .QuerySelectorAll("RailVehicles cOwnedEntity")
            .FirstOrDefault(element => element.GetAttribute("d:id") == vehicle.Id);

        if (railVehicle is null)
        {
            throw new Exception("unable to find rail vehicle in scenario document");
        }

        return railVehicle;
    }

    public static bool IsLeadVehicle(IElement railVehicle)
    {
        return railVehicle.ParentElement?.FirstElementChild == railVehicle;
    }
}
