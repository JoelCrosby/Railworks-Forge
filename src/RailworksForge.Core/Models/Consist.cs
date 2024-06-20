using AngleSharp.Dom;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models.Common;

namespace RailworksForge.Core.Models;

public class Consist : Blueprint
{
    public required string Id { get; init; }

    public required string LocomotiveName { get; init; }

    public string? LocoAuthor { get; init; }

    public LocoClass? LocoClass { get; set; }

    public required string ServiceName { get; init; }

    public bool PlayerDriver { get; init; }

    public required string ServiceId { get; init; }

    public static Consist Parse(IElement el)
    {
        var consistId = el.GetAttribute("d:id") ?? string.Empty;
        var locomotiveName = el.SelectTextContnet("LocoName English");
        var serviceName = el.SelectTextContnet("ServiceName English");
        var playerDriver = el.SelectTextContnet("PlayerDriver") == "1";
        var locoAuthor = el.SelectTextContnet("LocoAuthor");
        var blueprintId = el.SelectTextContnet("BlueprintID");
        var serviceId = el.SelectTextContnet("ServiceName Key");
        var locoClass = LocoClassUtils.Parse(el.SelectTextContnet("LocoClass"));
        var blueprintSetIdProduct = el.SelectTextContnet("LocoBP iBlueprintLibrary-cBlueprintSetID Product");
        var blueprintSetIdProvider = el.SelectTextContnet("LocoBP iBlueprintLibrary-cBlueprintSetID Provider");

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
