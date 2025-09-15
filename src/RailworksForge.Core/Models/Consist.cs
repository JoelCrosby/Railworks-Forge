using System.Diagnostics;

using AngleSharp.Dom;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models.Common;

namespace RailworksForge.Core.Models;

[DebuggerDisplay("{ServiceName}")]
public class Consist : Blueprint
{
    public required string Id { get; init; }

    public required string LocomotiveName { get; init; }

    public string SearchIndex { get; init; } = string.Empty;

    public string? LocoAuthor { get; init; }

    public LocoClass? LocoClass { get; init; }

    public required string ServiceName { get; init; }

    public bool PlayerDriver { get; init; }

    public required string ServiceId { get; init; }

    public AcquisitionState ConsistAcquisitionState { get; set; }

    public string? Number { get; init; }

    public int Length { get; init; }

    public required List<VehicleBlueprint> Vehicles { get; init; }

    public VehicleBlueprint? LeadVehicle {  get; init; }

    public int? Index { get; set; }

    public static Consist ParseConsist(IElement el)
    {
        var consistId = el.GetAttribute("d:id") ?? string.Empty;
        var locomotiveName = el.SelectLocalisedStringContent("LocoName");
        var serviceName = el.SelectLocalisedStringContent("ServiceName");
        var playerDriver = el.SelectTextContent("PlayerDriver") == "1";
        var locoAuthor = el.SelectTextContent("LocoAuthor");
        var blueprintId = el.SelectTextContent("BlueprintID");
        var serviceId = el.SelectTextContent("ServiceName Key");
        var locoClass = LocoClassUtils.Parse(el.SelectTextContent("LocoClass"));
        var blueprintSetIdProduct = el.SelectTextContent("LocoBP iBlueprintLibrary-cBlueprintSetID Product");
        var blueprintSetIdProvider = el.SelectTextContent("LocoBP iBlueprintLibrary-cBlueprintSetID Provider");

        Cache.ConsistAcquisitionStates.TryGetValue(consistId, out var cachedState);

        return new Consist
        {
            Id = consistId,
            LocomotiveName = locomotiveName,
            LocoAuthor = locoAuthor,
            LocoClass = locoClass,
            ServiceName = serviceName,
            PlayerDriver = playerDriver,
            BlueprintId = blueprintId,
            BlueprintSetIdProduct = blueprintSetIdProduct,
            BlueprintSetIdProvider = blueprintSetIdProvider,
            ServiceId = serviceId,
            ConsistAcquisitionState = cachedState ?? AcquisitionState.Missing,
            SearchIndex = $"{locomotiveName} {locoAuthor} {locoClass} {serviceName}".ToLowerInvariant(),
            Vehicles = [],
        };
    }

    public static Consist? ParseScenarioConsist(IElement el, int index)
    {
        var vehicles = el.QuerySelectorAll("RailVehicles cOwnedEntity").Select(VehicleBlueprint.Parse).ToList();
        var lead = GetLeadVehicle(vehicles);

        if (lead is null) return null;

        var consistId = el.GetAttribute("d:id") ?? string.Empty;
        var locomotiveName = lead.Element.SelectTextContent("Name");
        var playerDriver = el.SelectTextContent("Driver PlayerDriver") == "1";
        var blueprintId = lead.Element.SelectTextContent("BlueprintID iBlueprintLibrary-cAbsoluteBlueprintID BlueprintID");
        var serviceName = el.SelectLocalisedStringContent("Driver ServiceName");
        var serviceId = el.SelectTextContent("Driver ServiceName Key");
        var locoClass = LocoClassUtils.Parse(lead.Element.SelectTextContent("LocoClass"));
        var blueprintSetIdProduct = lead.Element.SelectTextContent("BlueprintID iBlueprintLibrary-cBlueprintSetID Product");
        var blueprintSetIdProvider = lead.Element.SelectTextContent("BlueprintID iBlueprintLibrary-cBlueprintSetID Provider");

        var consistAcquisitionState = GetConsistAcquisitionState(vehicles);

        return new Consist
        {
            Id = consistId,
            LeadVehicle = lead,
            LocomotiveName = locomotiveName,
            LocoAuthor = blueprintSetIdProvider,
            LocoClass = locoClass,
            ServiceName = serviceName,
            PlayerDriver = playerDriver,
            BlueprintId = blueprintId,
            BlueprintSetIdProduct = blueprintSetIdProduct,
            BlueprintSetIdProvider = blueprintSetIdProvider,
            ServiceId = serviceId,
            ConsistAcquisitionState = consistAcquisitionState,
            Length = vehicles.Count,
            SearchIndex = $"{locomotiveName} {blueprintSetIdProvider} {locoClass} {serviceName}".ToLowerInvariant(),
            Vehicles = vehicles,
            Index = index,
        };
    }

    private static VehicleBlueprint? GetLeadVehicle(List<VehicleBlueprint> vehicles)
    {
        if (vehicles.FirstOrDefault()?.BlueprintType == BlueprintType.Engine)
        {
            return vehicles.First();
        }

        if (vehicles.LastOrDefault()?.BlueprintType == BlueprintType.Engine)
        {
            return vehicles.Last();
        }

        return vehicles.FirstOrDefault();
    }

    private static AcquisitionState GetConsistAcquisitionState(List<VehicleBlueprint> vehicles)
    {
        var found = vehicles.Count(v => v.Blueprint.AcquisitionState is AcquisitionState.Found);

        var partial = found > 0 && found < vehicles.Count;
        var all = found == vehicles.Count;

        if (all) return AcquisitionState.Found;
        if (partial) return AcquisitionState.Partial;

        return AcquisitionState.Missing;
    }

    public static IElement? GetServiceConsist(IDocument document, Consist consist)
    {
        var byId = document.QuerySelectorAll("cConsist").FirstOrDefault(el => el.GetAttribute("d:id") == consist.Id);

        if (byId is not null)
        {
            return byId;
        }

        return document.QuerySelectorAll("cConsist").QueryByTextContent("ServiceName Key", consist.ServiceId);
    }
}
