using System.Diagnostics;

using AngleSharp.Dom;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models.Common;

using Serilog;

namespace RailworksForge.Core.Models;

[DebuggerDisplay("{ServiceName}")]
public record Consist : Blueprint
{
    public required string Id { get; init; }

    public required string LocomotiveName { get; init; }

    public string? LocoAuthor { get; init; }

    public LocoClass? LocoClass { get; init; }

    public required string ServiceName { get; init; }

    public bool PlayerDriver { get; init; }

    public required string ServiceId { get; init; }

    public AcquisitionState ConsistAcquisitionState { get; set; }

    public string? Number { get; init; }


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

        var consistId = lead.GetAttribute("d:id") ?? string.Empty;
        var locomotiveName = lead.SelectTextContent("Name");
        var playerDriver = el.SelectTextContent("Driver PlayerDriver") == "1";
        var blueprintId = lead.SelectTextContent("BlueprintID iBlueprintLibrary-cAbsoluteBlueprintID BlueprintID");
        var serviceName = el.SelectLocalisedStringContent("Driver ServiceName");
        var serviceId = el.SelectTextContent("Driver ServiceName Key");
        var locoClass = LocoClassUtils.Parse(lead.SelectTextContent("LocoClass"));
        var blueprintSetIdProduct = lead.SelectTextContent("BlueprintID iBlueprintLibrary-cBlueprintSetID Product");
        var blueprintSetIdProvider = lead.SelectTextContent("BlueprintID iBlueprintLibrary-cBlueprintSetID Provider");

        Cache.AcquisitionStates.TryGetValue(consistId, out var cachedState);

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
            ConsistAcquisitionState = cachedState,
        };
    }
}
