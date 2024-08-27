using AngleSharp.Dom;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models;

namespace RailworksForge.Core;

public record GeneratedVehicle(IElement Element, string Number);

public class VehicleGenerator
{
    public static async Task<GeneratedVehicle> GenerateVehicle(IDocument document, IElement prevElem, ScenarioConsist vehicle, ConsistEntry consistVehicle)
    {
        var vehicleType = await GetConsistEntryVehicleType(consistVehicle);
        var xml = VehicleTemplates.GetXml(vehicleType);
        var doc = await XmlParser.ParseDocumentAsync(xml);
        var cOwnedEntity = doc.DocumentElement;

        if (cOwnedEntity is null)
        {
            throw new Exception("could not find root element for vehicle type");
        }

        var typeSpecificElement = vehicle.BlueprintType switch
        {
            BlueprintType.Engine => cOwnedEntity.QuerySelector("Component cEngine"),
            BlueprintType.Wagon => cOwnedEntity.QuerySelector("Component cWagon"),
            BlueprintType.Tender => cOwnedEntity.QuerySelector("Component cTender"),
            _ =>  throw new Exception("encountered unknown vehicle type"),
        };

        if (typeSpecificElement is null)
        {
            throw new Exception("could not find type specific element for vehicle type");
        }

        AddFollowers(prevElem, typeSpecificElement);
        AddEntityContainers(document, vehicle, cOwnedEntity);
        AddCargoComponents(document, vehicle, cOwnedEntity);

        UpdateComponentCpos(prevElem, vehicle, cOwnedEntity);
        UpdateEntityId(document, cOwnedEntity);
        UpdateOwnedEntityIds(cOwnedEntity);
        UpdateBlueprintIds(vehicle, cOwnedEntity);
        UpdateFlipped(cOwnedEntity, consistVehicle);
        UpdateMass(cOwnedEntity, vehicle);

        if (vehicle.IsReskin)
        {
            UpdateReskinBlueprintIds(vehicle, cOwnedEntity);
        }

        cOwnedEntity.UpdateTextElement("Name", vehicle.Name!);

        var originalNumber = cOwnedEntity.SelectTextContent("UniqueNumber");
        var number = vehicle.Number ?? GetAvailableNumber(vehicle, consistVehicle);

        cOwnedEntity.UpdateTextElement("UniqueNumber", number);

        UpdateOperationNumbers(doc, number, originalNumber);

        return new GeneratedVehicle(cOwnedEntity, number);
    }

    private static async Task<BlueprintType> GetConsistEntryVehicleType(ConsistEntry consistVehicle)
    {
        var document = await consistVehicle.GetXmlDocument();
        var elementName = document.QuerySelector("Blueprint")?.FirstElementChild?.NodeName;

        return Utilities.ParseBlueprintType(elementName);
    }

    private static void UpdateOperationNumbers(IDocument doc, string number, string originalNumber)
    {
        var cConsistOperations = doc.QuerySelector("cConsistOperations");

        if (cConsistOperations is null) return;

        var operationTargetNumbers = cConsistOperations
            .QuerySelectorAll("DeltaTarget cDriverInstructionTarget RailVehicleNumber e")
            .Where(item => item.Text() == originalNumber);

        foreach (var targetNumber in operationTargetNumbers)
        {
            targetNumber.SetTextContent(number);
        }
    }

    private static void UpdateMass(IElement cOwnedEntity, ScenarioConsist consist)
    {
        if (consist.Mass.ToString() is {} mass)
        {
            cOwnedEntity.QuerySelector("TotalMass")?.SetTextContent(mass);
        }
    }

    private static string GetAvailableNumber(ScenarioConsist vehicle, ConsistEntry consistEntry)
    {
        var path = vehicle.NumberingListPath;

        if (path is null) throw new Exception("could not find path for numbers csv");

        var normalisedPath = path.Replace('\\', Path.DirectorySeparatorChar) + ".dcsv";
        var filepath = Path.Join(Paths.GetAssetsDirectory(), normalisedPath);

        var text = Paths.Exists(filepath) ? File.ReadAllText(filepath) : GetCompressedText();

        if (text is null)
        {
            throw new Exception($"could not find part of path '{path}'");
        }

        var document = XmlParser.ParseDocument(text);
        var element = document.QuerySelector("cCSVItem Name");

        return element?.TextContent ?? throw new Exception($"could not read number from csv in path '{filepath}'");

        string? GetCompressedText()
        {
            return GetCompressedNumberingList(consistEntry, normalisedPath);
        }
    }

    private static string? GetCompressedNumberingList(ConsistEntry consistEntry, string path)
    {
        var productPath = Path.Join(consistEntry.Blueprint.BlueprintSetIdProvider, consistEntry.Blueprint.BlueprintSetIdProduct);
        var fullProductPath = Path.Join(Paths.GetAssetsDirectory(), productPath);
        var productArchives = Directory.EnumerateFiles(fullProductPath, "*.ap", SearchOption.TopDirectoryOnly);
        var normalisedPath = path.Replace(productPath, string.Empty);

        foreach (var productArchive in productArchives)
        {
            var file = Archives.TryGetTextFileContentFromPath(productArchive, normalisedPath);

            if (file is null) continue;

            return file;
        }

        return null;
    }

    private static void AddFollowers(IElement prevElem, IElement typeSpecificElement)
    {
        var followers = typeSpecificElement.QuerySelector("Followers");

        if (followers is null)
        {
            throw new Exception("could not find followers vehicle");
        }

        var prevFollowers = prevElem
            .QuerySelector("Component")?
            .QuerySelectorAll("Followers").FirstOrDefault()?
            .QuerySelectorAll("Network-cTrackFollower");

        if (prevFollowers is null)
        {
            throw new Exception("could not find previous followers vehicle");
        }

        foreach (var cTrackFollower in prevFollowers)
        {
            var newFollower = cTrackFollower.Clone();
            followers.AppendChild(newFollower);
        }
    }

    private static void AddEntityContainers(IDocument document, ScenarioConsist vehicle, IElement cOwnedEntity)
    {
        var cEntityContainer = cOwnedEntity.QuerySelector("Component cEntityContainer StaticChildrenMatrix");

        if (cEntityContainer is null) return;

        for (var i = 0; i < vehicle.EntityCount; i++)
        {
            var newNode = document.GenerateEntityContainerItem();
            cEntityContainer.AppendChild(newNode);
        }
    }

    private static void AddCargoComponents(IDocument document, ScenarioConsist vehicle, IElement cOwnedEntity)
    {
        var cCargoComponent = cOwnedEntity.QuerySelector("Component cCargoComponent InitialLevel");

        if (cCargoComponent is null) return;

        for (var i = 0; i < vehicle.CargoCount; i++)
        {
            var value = vehicle.CargoComponents[i].Value;
            var altEncoding = vehicle.CargoComponents[i].AltEncoding;

            var newNode = document.GenerateCargoComponentItem(value,altEncoding);

            cCargoComponent.AppendChild(newNode);
        }
    }

    private static void UpdateComponentCpos(IElement prevElem, ScenarioConsist vehicle, IElement cOwnedEntity)
    {
        var cPosOri = prevElem.QuerySelector("Component cPosOri")?.Clone();

        if (cPosOri is null)
        {
            throw new Exception("could not get the cPos from the previous vehicle");
        }

        var cAnimObjectRender = cOwnedEntity.QuerySelector("cAnimObjectRender");

        if (cAnimObjectRender is null)
        {
            throw new Exception("could not get cAnimObjectRender from template vehicle document");
        }

        cAnimObjectRender.InsertAfter(cPosOri);

        cOwnedEntity.QuerySelector("Name")?.SetTextContent(vehicle.Name ?? string.Empty);
    }

    private static void UpdateBlueprintIds(ScenarioConsist vehicle, IElement cOwnedEntity)
    {
        var cAbsoluteBlueprintId = cOwnedEntity
            .QuerySelector("BlueprintID iBlueprintLibrary-cAbsoluteBlueprintID");

        if (cAbsoluteBlueprintId is null) return;

        cAbsoluteBlueprintId
            .QuerySelector("BlueprintSetID iBlueprintLibrary-cBlueprintSetID Provider")?
            .SetTextContent(vehicle.BlueprintSetIdProvider);

        cAbsoluteBlueprintId
            .QuerySelector("BlueprintSetID iBlueprintLibrary-cBlueprintSetID Product")?
            .SetTextContent(vehicle.BlueprintSetIdProduct);

        cAbsoluteBlueprintId.QuerySelector("BlueprintID")?.SetTextContent(vehicle.BlueprintId);
    }

    private static void UpdateFlipped(IElement cOwnedEntity, ConsistEntry consistEntry)
    {
        var flipped = consistEntry.Flipped ? "1" : "0";

        cOwnedEntity.QuerySelector("Flipped")?.SetTextContent(flipped);
    }

    private static void UpdateEntityId(IDocument document, IElement cOwnedEntity)
    {
        var entityId = cOwnedEntity.QuerySelector("EntityID");
        entityId?.AppendChild(document.GenerateCGuid());
    }

    private static void UpdateOwnedEntityIds(IElement cOwnedEntity)
    {
        var idElements = cOwnedEntity
            .DescendantsAndSelf<IElement>()
            .Where(elem => elem.GetAttribute("d:id") == string.Empty);

        var idRandom = new Random();

        foreach (var elem in idElements)
        {
            var id = idRandom.Next(100000000, 999999999);
            elem.RemoveAttribute("d:id");
            elem.SetAttribute(Utilities.NS, "d:id", id.ToString());
        }
    }

    private static void UpdateReskinBlueprintIds(ScenarioConsist vehicle, IElement cOwnedEntity)
    {
        var reskinAbsoluteBlueprintId = cOwnedEntity
            .QuerySelector("ReskinBlueprintID iBlueprintLibrary-cAbsoluteBlueprintID");

        if (reskinAbsoluteBlueprintId is null) return;

        reskinAbsoluteBlueprintId
            .QuerySelector("BlueprintSetID iBlueprintLibrary-cBlueprintSetID Provider")?
            .SetTextContent(vehicle.ReskinBlueprintSetIdProvider!);

        reskinAbsoluteBlueprintId
            .QuerySelector("BlueprintSetID iBlueprintLibrary-cBlueprintSetID Product")?
            .SetTextContent(vehicle.ReskinBlueprintSetIdProduct!);

        reskinAbsoluteBlueprintId
            .QuerySelector("BlueprintID BlueprintID")?
            .SetTextContent(vehicle.ReskinBlueprintId!);
    }
}
