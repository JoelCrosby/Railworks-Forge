using System.Diagnostics;

using AngleSharp.Dom;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models.Common;

using Serilog;

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

    public required List<Blueprint> Vehicles { get; init; }

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

        Cache.AcquisitionStates.TryGetValue(consistId, out var cachedState);

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
            ConsistAcquisitionState = cachedState,
            SearchIndex = $"{locomotiveName} {locoAuthor} {locoClass} {serviceName}".ToLowerInvariant(),
            Vehicles = [],
        };
    }

    public static Consist? ParseScenarioConsist(IElement el)
    {
        var railVehicles = el.QuerySelector("RailVehicles");

        if (railVehicles is null)
        {
            Log.Information("could not RailVehicles element");
            return null;
        }

        var lead = railVehicles.QuerySelector("cOwnedEntity");

        if (lead is null)
        {
            Log.Information("could not find leadVehicle element");
            return null;
        }

        var consistId = el.GetAttribute("d:id") ?? string.Empty;
        var locomotiveName = lead.SelectTextContent("Name");
        var playerDriver = el.SelectTextContent("Driver PlayerDriver") == "1";
        var blueprintId = lead.SelectTextContent("BlueprintID iBlueprintLibrary-cAbsoluteBlueprintID BlueprintID");
        var serviceName = el.SelectLocalisedStringContent("Driver ServiceName");
        var serviceId = el.SelectTextContent("Driver ServiceName Key");
        var locoClass = LocoClassUtils.Parse(lead.SelectTextContent("LocoClass"));
        var blueprintSetIdProduct = lead.SelectTextContent("BlueprintID iBlueprintLibrary-cBlueprintSetID Product");
        var blueprintSetIdProvider = lead.SelectTextContent("BlueprintID iBlueprintLibrary-cBlueprintSetID Provider");

        var vehicles = el.QuerySelectorAll("RailVehicles BlueprintID iBlueprintLibrary-cAbsoluteBlueprintID").Select(Parse).ToList();
        var consistAcquisitionState = vehicles.All(v => v.AcquisitionState is AcquisitionState.Found)
            ? AcquisitionState.Found : AcquisitionState.Missing;

        return new Consist
        {
            Id = consistId,
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
        };
    }

    public static IElement? GetServiceConsist(IDocument document, Consist consist)
    {
        var byId = document.QuerySelectorAll("cConsist").FirstOrDefault(el => el.GetAttribute("d:id") == consist.Id);

        if (byId is not null) return byId;

        return document.QuerySelectorAll("cConsist").QueryByTextContent("ServiceName Key", consist.ServiceId);
    }
}
