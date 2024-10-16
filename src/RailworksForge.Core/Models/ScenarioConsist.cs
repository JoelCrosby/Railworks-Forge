using AngleSharp.Dom;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models.Common;

namespace RailworksForge.Core.Models;

public record ScenarioConsist : Consist
{
    public string? Name { get; init; }

    public required BlueprintType BlueprintType { get; init; }

    public required int EntityCount { get; init; }

    public required List<CargoComponent> CargoComponents { get; init; }

    public int CargoCount => CargoComponents.Count;

    public bool IsReskin { get; init; }

    public string? ReskinBlueprintId { get; init; }

    public string? ReskinBlueprintSetIdProvider { get; init; }

    public string? ReskinBlueprintSetIdProduct { get; init; }

    public string? NumberingListPath { get; init; }

    public float? Mass { get; init; }

    public static ScenarioConsist ParseConsist(IDocument document, Blueprint blueprint)
    {
        var el = document.DocumentElement;

        if (el is null)
        {
            throw new Exception("could not find cBlueprintLoader element in document");
        }

        var consist = Consist.ParseConsist(el);

        var vehicleType = Utilities.ParseBlueprintType(el.QuerySelector("Blueprint")?.FirstElementChild?.NodeName);
        var entityCount = el.QuerySelectorAll("cEntityContainerBlueprint-sChild").Length;
        var cargoComponents = GetCargoComponents(el);

        var name = el.SelectTextContent("cEngineBlueprint Name");
        var numberList = el.QuerySelector("NumberingList CsvFile")?.Text();

        _ = float.TryParse(el.QuerySelector("Mass")?.Text(), out var mass);

        return new ScenarioConsist
        {
            Id = consist.Id,
            Name = name,
            LocomotiveName = consist.LocomotiveName,
            LocoAuthor = consist.LocoAuthor,
            LocoClass = consist.LocoClass,
            ServiceName = consist.ServiceName,
            PlayerDriver = consist.PlayerDriver,
            BlueprintId = blueprint.BlueprintId,
            BlueprintSetIdProduct = blueprint.BlueprintSetIdProduct,
            BlueprintSetIdProvider = blueprint.BlueprintSetIdProvider,
            BlueprintType = vehicleType,
            ServiceId = consist.ServiceId,
            EntityCount = entityCount,
            CargoComponents = cargoComponents,
            NumberingListPath = numberList,
            Mass = mass,
        };
    }

    private static List<CargoComponent> GetCargoComponents(IElement element)
    {
        var components = new List<CargoComponent>();
        var cargoDef = element.QuerySelector("CargoDef");

        if (cargoDef is null) return components;

        foreach (var cBulkCargoDef in cargoDef.Children)
        {
            var capacity = cBulkCargoDef.QuerySelector("Capacity");
            var component = new CargoComponent("0", "0000000000000000");

            if (capacity is not null)
            {
                var alt = capacity.GetAttribute("alt_encoding") ?? string.Empty;

                component = new CargoComponent(capacity.TextContent, alt);
            }

            components.Add(component);
        }

        return components;
    }
}

public record CargoComponent(string Value, string AltEncoding);
