using System.Diagnostics;
using System.IO.Compression;

using AngleSharp.Dom;
using AngleSharp.Xml.Dom;

using RailworksForge.Core.Exceptions;
using RailworksForge.Core.Extensions;
using RailworksForge.Core.External;
using RailworksForge.Core.Models;

namespace RailworksForge.Core;

public class ConsistService
{
    public static async Task ReplaceConsist(Consist target, PreloadConsist preload, Scenario scenario)
    {
        var outputDirectory = scenario.DirectoryPath;
        var backupOutputDirectory = Directory.GetParent(outputDirectory)!.FullName;
        var backupPath = Path.Join(backupOutputDirectory, $"backup-{DateTimeOffset.UtcNow:dd-MMM-yy_hh-mm}.zip");

        ZipFile.CreateFromDirectory(outputDirectory, backupPath);

        var scenarioDocument = await GetUpdatedScenario(scenario, target, preload);
        var scenarioPropertiesDocument = await GetUpdatedScenarioProperties(scenario, target, preload);

        await WriteScenarioDocument(outputDirectory, scenarioDocument);
        await WriteScenarioPropertiesDocument(outputDirectory, scenarioPropertiesDocument);

        ClearCache();
    }

    private static async Task<IXmlDocument> GetUpdatedScenario(Scenario scenario, Consist target, PreloadConsist preload)
    {
        var document = await scenario.GetXmlDocument();

        var scenarioConsist = document
            .QuerySelectorAll("cConsist")
            .QueryByTextContent("Driver ServiceName Key", target.ServiceId);

        if (scenarioConsist is null)
        {
            throw new Exception("unable to find scenario consist");
        }

        var blueprintNodes = scenarioConsist.QuerySelectorAll("RailVehicles cOwnedEntity");
        var nodeCountToKeep = preload.ConsistEntries.Count;

        await Parallel.ForEachAsync(preload.ConsistEntries, async (entry, _) => await entry.GetXmlDocument());

        for (var i = 0; i < blueprintNodes.Length; i++)
        {
            var scenarioNode = blueprintNodes[i];

            if (i >= nodeCountToKeep)
            {
                scenarioNode.RemoveFromParent();
                continue;
            }

            var blueprintNode = preload.ConsistEntries[i];
            var blueprintBinDocument = await blueprintNode.GetXmlDocument();

            var blueprintName = blueprintBinDocument.SelectTextContent("Blueprint Name");

            var name = scenarioNode.QuerySelector("Name");
            var blueprint = scenarioNode.QuerySelector("BlueprintID iBlueprintLibrary-cAbsoluteBlueprintID");

            if (blueprint is null) continue;

            var scenarioBlueprintId = blueprint.QuerySelector("BlueprintID");
            var scenarioProvider = blueprint.QuerySelector("BlueprintSetID iBlueprintLibrary-cBlueprintSetID Provider");
            var scenarioProduct = blueprint.QuerySelector("BlueprintSetID iBlueprintLibrary-cBlueprintSetID Product");


            if (scenarioProvider is null || scenarioProduct is null || scenarioBlueprintId is null)
            {
                continue;
            }

            name?.SetTextContent(blueprintName);

            scenarioProvider.SetTextContent(blueprintNode.BlueprintIdProvider);
            scenarioProduct.SetTextContent(blueprintNode.BlueprintIdProduct);
            scenarioBlueprintId.SetTextContent(blueprintNode.BlueprintId);
        }

        XmlException.ThrowIfDocumentInvalid(document);

        return document;
    }

    private static async Task<IXmlDocument> GetUpdatedScenarioProperties(Scenario scenario, Consist target, PreloadConsist preload)
    {
        var document = await scenario.GetPropertiesXmlDocument();

        var serviceElement = document
            .QuerySelectorAll("sDriverFrontEndDetails")
            .QueryByTextContent("ServiceName Key", target.ServiceId);

        if (serviceElement is null)
        {
            throw new Exception($"could not find service {target.ServiceName} in scenario properties file.");
        }

        UpdateBlueprint(serviceElement, preload);
        UpdateFilePath(serviceElement, preload);
        UpdatePreloadElement(document, preload);

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
        if (document.QuerySelector(tagSelector) is not {} preloadElement)
        {
            return;
        }

        var needle = $"{preload.BlueprintIdProvider}:{preload.BlueprintIdProduct}".ToLowerInvariant();
        var providerProductSet = preloadElement
            .QuerySelectorAll("iBlueprintLibrary-cBlueprintSetID")
            .Aggregate(
                new Dictionary<string, int>(), (acc, curr) =>
                {
                    var provider = curr.SelectTextContent("Provider");
                    var product = curr.SelectTextContent("Product");
                    var index = $"{provider}:{product}".ToLowerInvariant();

                    var textIdValue = curr.GetAttribute("d:id");
                    var isIntId = int.TryParse(textIdValue, out var idValue);
                    var id = isIntId ? idValue : -1;

                    acc.TryAdd(index, id);

                    return acc;
                }
            );

        if (providerProductSet.ContainsKey(needle))
        {
            return;
        }

        var entryId = providerProductSet.OrderByDescending(pair => pair.Value).Select(p => p.Value).First() + 2;
        var markup =
            $"""
             <iBlueprintLibrary-cBlueprintSetID d:id="{entryId}">
                <Provider d:type="cDeltaString">{preload.BlueprintIdProvider}</Provider>
                <Product d:type="cDeltaString">{preload.BlueprintIdProduct}</Product>
             </iBlueprintLibrary-cBlueprintSetID>
             """;

        preloadElement.InnerHtml += markup;
    }

    private static async Task WriteScenarioDocument(string path, IXmlDocument document)
    {
        const string filename = "Scenario.bin.xml";
        const string binFilename = "Scenario.bin";

        var destination = Path.Join(path, filename);
        var binDestination = Path.Join(path, binFilename);

        File.Delete(destination);

        await document.ToXmlAsync(destination);

        File.Delete(binDestination);

        await Serz.Convert(destination);
        await Paths.CreateMd5HashFile(binDestination);
    }

    private static async Task WriteScenarioPropertiesDocument(string path, IXmlDocument document)
    {
        const string filename = "ScenarioProperties.xml";
        var destination = Path.Join(path, filename);

        File.Delete(destination);

        await document.ToXmlAsync(destination);
        await Paths.CreateMd5HashFile(destination);
    }

    private static void ClearCache()
    {
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
