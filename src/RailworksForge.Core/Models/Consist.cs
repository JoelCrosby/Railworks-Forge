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

    public string? LocoAuthor { get; init; }

    public LocoClass? LocoClass { get; set; }

    public required string ServiceName { get; init; }

    public bool PlayerDriver { get; init; }

    public required string ServiceId { get; init; }

    public AcquisitionState ConsistAcquisitionState { get; set; }

    public static Consist ParseConsist(IElement el)
    {
        var consistId = el.GetAttribute("d:id") ?? string.Empty;
        var locomotiveName = el.SelectTextContent("LocoName English");
        var serviceName = el.SelectTextContent("ServiceName English");
        var playerDriver = el.SelectTextContent("PlayerDriver") == "1";
        var locoAuthor = el.SelectTextContent("LocoAuthor");
        var blueprintId = el.SelectTextContent("BlueprintID");
        var serviceId = el.SelectTextContent("ServiceName Key");
        var locoClass = LocoClassUtils.Parse(el.SelectTextContent("LocoClass"));
        var blueprintSetIdProduct = el.SelectTextContent("LocoBP iBlueprintLibrary-cBlueprintSetID Product");
        var blueprintSetIdProvider = el.SelectTextContent("LocoBP iBlueprintLibrary-cBlueprintSetID Provider");

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
        };
    }
}
