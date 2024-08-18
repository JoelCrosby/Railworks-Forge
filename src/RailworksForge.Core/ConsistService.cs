using System.Diagnostics;

using AngleSharp.Dom;
using AngleSharp.Xml.Dom;

using RailworksForge.Core.Exceptions;
using RailworksForge.Core.Extensions;
using RailworksForge.Core.External;
using RailworksForge.Core.Models;

using Serilog;

namespace RailworksForge.Core;

public class ConsistService
{
    public static async Task ReplaceConsist(TargetConsist target, PreloadConsist preload, Scenario scenario)
    {
        var scenarioDocument = await GetUpdatedScenario(scenario, target, preload);
        var scenarioPropertiesDocument = await GetUpdatedScenarioProperties(scenario, target, preload);

        scenario.CreateBackup();

        await WriteScenarioDocument(scenario, scenarioDocument);
        await WriteScenarioPropertiesDocument(scenario, scenarioPropertiesDocument);

        ClearCache(scenario);
    }

    public static async Task DeleteConsist(TargetConsist target, Scenario scenario)
    {
        var scenarioDocument = await GetDeleteUpdatedScenario(scenario, target);
        var scenarioPropertiesDocument = await GetDeleteScenarioProperties(scenario, target);

        scenario.CreateBackup();

        await WriteScenarioDocument(scenario, scenarioDocument);
        await WriteScenarioPropertiesDocument(scenario, scenarioPropertiesDocument);

        ClearCache(scenario);
    }

    private static async Task<IXmlDocument> GetDeleteUpdatedScenario(Scenario scenario, TargetConsist target)
    {
        var document = await scenario.GetXmlDocument(false);

        foreach (var consist in target.GetConsists())
        {
            var consistElement = document
                .QuerySelectorAll("cConsist")
                .QueryByTextContent("Driver ServiceName Key", consist.ServiceId);

            if (consistElement is null)
            {
                throw new Exception("unable to find scenario consist");
            }

            consistElement.RemoveFromParent();
        }

        XmlException.ThrowIfDocumentInvalid(document);

        return document;
    }

    private static async Task<IXmlDocument> GetDeleteScenarioProperties(Scenario scenario, TargetConsist target)
    {
        var document = await scenario.GetPropertiesXmlDocument();

        foreach (var consist in target.GetConsists())
        {
            var element = document
                .QuerySelectorAll("sDriverFrontEndDetails")
                .QueryByTextContent("ServiceName Key", consist.ServiceId);

            if (element is null)
            {
                throw new Exception($"could not find service {consist.ServiceName} in scenario properties file.");
            }

            element.RemoveFromParent();
        }

        XmlException.ThrowIfDocumentInvalid(document);

        return document;
    }

    private static async Task<IXmlDocument> GetUpdatedScenario(Scenario scenario, TargetConsist target, PreloadConsist preload)
    {
        var document = await scenario.GetXmlDocument(false);

        foreach (var consist in target.GetConsists())
        {
            var serviceConsist = document
                .QuerySelectorAll("cConsist")
                .QueryByTextContent("Driver ServiceName Key", consist.ServiceId);

            if (serviceConsist is null)
            {
                throw new Exception("unable to find scenario consist");
            }

            var railVehicles = serviceConsist.QuerySelector("RailVehicles");

            if (railVehicles is null)
            {
                throw new Exception("unable to find rail vehicles in scenario document");
            }

            var blueprintNodes = serviceConsist.QuerySelectorAll("RailVehicles cOwnedEntity");

            if (blueprintNodes.First().Clone() is not IElement firstBlueprint)
            {
                throw new Exception("unable to clone first blueprint");
            }

            await Parallel.ForEachAsync(preload.ConsistEntries, async (entry, _) => await entry.GetXmlDocument());

            var selectedConsistLength = preload.ConsistEntries.Count;

            foreach (var node in blueprintNodes)
            {
                node.RemoveFromParent();
            }

            var cDriver = serviceConsist.QuerySelector("Driver cDriver");

            if (cDriver is null)
            {
                throw new Exception("driver element not found");
            }

            var previousVehicle = firstBlueprint;
            var initialRv = cDriver.QuerySelector("InitialRV");

            if (initialRv is null)
            {
                throw new Exception("could not find initialRV element");
            }

            foreach (var child in initialRv.Children)
            {
                child.RemoveFromParent();
            }

            for (var i = 0; i < selectedConsistLength; i++)
            {
                var consistVehicle = preload.ConsistEntries[i];
                var railVehicle = await AddConsistVehicle(consistVehicle, document, previousVehicle);

                if (railVehicle is null)
                {
                    throw new Exception("failed to create rail vehicle for consist");
                }

                var e = document.CreateXmlElement("e");
                e.SetAttribute("type", "cDeltaString");
                e.SetTextContent(railVehicle.Number);

                initialRv.AppendChild(e);

                railVehicles.AppendChild(railVehicle.Element);
                previousVehicle = railVehicle.Element;
            }
        }

        XmlException.ThrowIfDocumentInvalid(document);

        return document;
    }

    private static async Task<GeneratedVehicle> AddConsistVehicle(ConsistEntry consistVehicle, IXmlDocument document, IElement scenarioNode)
    {
        var vehicleDocument = await consistVehicle.GetXmlDocument();
        var scenarioConsist = ScenarioConsist.ParseConsist(vehicleDocument, consistVehicle);

        return VehicleGenerator.GenerateVehicle(document, scenarioNode, scenarioConsist, consistVehicle);
    }

    private static async Task<IXmlDocument> GetUpdatedScenarioProperties(Scenario scenario, TargetConsist target, PreloadConsist preload)
    {
        var document = await scenario.GetPropertiesXmlDocument();

        foreach (var consist in target.GetConsists())
        {
            var serviceElement = document
                .QuerySelectorAll("sDriverFrontEndDetails")
                .QueryByTextContent("ServiceName Key", consist.ServiceId);

            if (serviceElement is null)
            {
                throw new Exception($"could not find service {consist.ServiceName} in scenario properties file.");
            }

            UpdateBlueprint(serviceElement, preload);
            UpdateFilePath(serviceElement, preload);
            UpdatePreloadElement(document, preload);
        }

        XmlException.ThrowIfDocumentInvalid(document);

        return document;
    }

    private static void UpdateBlueprint(IElement serviceElement, PreloadConsist preload)
    {
        serviceElement.UpdateTextElement("LocoName Key", Guid.NewGuid().ToString());
        serviceElement.UpdateTextElement("LocoName English", preload.LocomotiveName);
        serviceElement.UpdateTextElement("LocoBP iBlueprintLibrary-cAbsoluteBlueprintID BlueprintID", preload.BlueprintId);
        serviceElement.UpdateTextElement("LocoBP iBlueprintLibrary-cAbsoluteBlueprintID BlueprintSetID iBlueprintLibrary-cBlueprintSetID Provider", preload.BlueprintIdProvider);
        serviceElement.UpdateTextElement("LocoBP iBlueprintLibrary-cAbsoluteBlueprintID BlueprintSetID iBlueprintLibrary-cBlueprintSetID Product", preload.BlueprintIdProduct);
        serviceElement.UpdateTextElement("LocoClass", LocoClassUtils.ToLongFormString(preload.EngineType));
        serviceElement.UpdateTextElement("LocoAuthor", preload.BlueprintIdProvider);
    }

    private static void UpdateFilePath(IElement serviceElement, PreloadConsist preload)
    {
        if (serviceElement.QuerySelector("FilePath") is not { } filePath)
        {
            return;
        }

        var parts = preload.BlueprintId.Split('\\');
        var partsWithoutFilename = parts[..^1];
        var blueprintDirectory = string.Join('\\', partsWithoutFilename);
        var packagedPath = $@"{preload.BlueprintIdProvider}\{preload.BlueprintIdProduct}\{blueprintDirectory}";

        filePath.SetTextContent(packagedPath);
    }

    private static void UpdatePreloadElement(IXmlDocument document, PreloadConsist preload)
    {
        UpdateBlueprintSetCollection(document, preload, "RBlueprintSetPreLoad");
        UpdateBlueprintSetCollection(document, preload, "RequiredSet");
    }

    private static void UpdateBlueprintSetCollection(IXmlDocument document, PreloadConsist preload, string tagSelector)
    {
        if (document.QuerySelector(tagSelector) is not {} collectionElement)
        {
            return;
        }

        var needle = $"{preload.BlueprintIdProvider}:{preload.BlueprintIdProduct}".ToLowerInvariant();
        var providerProductSet = collectionElement
            .QuerySelectorAll("iBlueprintLibrary-cBlueprintSetID")
            .Aggregate(new Dictionary<string, int>(), (acc, curr) =>
            {
                var provider = curr.SelectTextContent("Provider");
                var product = curr.SelectTextContent("Product");
                var index = $"{provider}:{product}".ToLowerInvariant();

                var textIdValue = curr.GetAttribute("d:id");
                var isIntId = int.TryParse(textIdValue, out var idValue);
                var id = isIntId ? idValue : -1;

                acc.TryAdd(index, id);

                return acc;
            });

        if (providerProductSet.ContainsKey(needle))
        {
            return;
        }

        var currentEntryId = providerProductSet.OrderByDescending(pair => pair.Value).Select(p => p.Value).FirstOrDefault();
        var initialId = currentEntryId == 0 ? new Random().Next(10000, 99999) : currentEntryId;
        var entryId = initialId + 2;

        var setElement = CreateBlueprintSetElement(document, preload, entryId);

        collectionElement.AppendChild(setElement);
    }

    private static IElement CreateBlueprintSetElement(IXmlDocument document, PreloadConsist preload, int entryId)
    {
        var setElement = document.CreateXmlElement("iBlueprintLibrary-cBlueprintSetID");
        setElement.SetAttribute("d:id", entryId.ToString());

        var providerElement = document.CreateXmlElement("Provider");
        providerElement.SetAttribute("d:type", "cDeltaString");
        providerElement.SetTextContent(preload.BlueprintIdProvider);

        var productElement = document.CreateXmlElement("Product");
        productElement.SetAttribute("d:type", "cDeltaString");
        productElement.SetTextContent(preload.BlueprintIdProduct);

        setElement.AppendChild(providerElement);
        setElement.AppendChild(productElement);

        return setElement;
    }

    private static async Task WriteScenarioDocument(Scenario scenario, IXmlDocument document)
    {
        const string filename = "Scenario.bin.xml";
        const string binFilename = "Scenario.bin";

        var destination = GetScenarioPathForFilename(scenario, filename);
        var binDestination = GetScenarioPathForFilename(scenario, binFilename);

        File.Delete(destination);

        await document.ToXmlAsync(destination);

        File.Delete(binDestination);

        var converted = await Serz.Convert(destination, true);

        File.Copy(converted.OutputPath, binDestination);

        File.Delete(converted.OutputPath);
        File.Delete(destination);

        await Paths.CreateMd5HashFile(binDestination);
    }

    private static async Task WriteScenarioPropertiesDocument(Scenario scenario, IXmlDocument document)
    {
        const string filename = "ScenarioProperties.xml";

        var destination = GetScenarioPathForFilename(scenario, filename);

        File.Delete(destination);

        await document.ToXmlAsync(destination);
        await Paths.CreateMd5HashFile(destination);
    }

    private static string GetScenarioPathForFilename(Scenario scenario, string filename)
    {
        return scenario.PackagingType is PackagingType.Unpacked
            ? Path.Join(scenario.DirectoryPath, filename)
            : Path.Join(scenario.DirectoryPath, "Scenarios", scenario.Id, filename);
    }

    private static void ClearCache(Scenario scenario)
    {
        try
        {
            File.Delete(scenario.CachedDocumentPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "failed to delete cached scenario document");
        }

        var directory = Paths.GetRoutesDirectory();

        var files = new []
        {
            "RVDBCache.bin",
            "RVDBCache.bin.MD5",
            "SDBCache.bin",
            "SDBCache.bin.MD5",
            "TMCache.dat",
            "TMCache.dat.MD5",
        };

        foreach (var file in files)
        {
            TryDeleteFile(directory, file);
        }

        return;

        static void TryDeleteFile(string dir, string filename)
        {
            var path = Path.Join(dir, filename);

            try
            {
                File.Delete(path);
            }
            catch (Exception e)
            {
                Debug.WriteLine("failed to delete file at path {path}", e.Message);
            }
        }
    }
}
