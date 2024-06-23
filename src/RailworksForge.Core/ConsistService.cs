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

            if (name is not null)
            {
                name.SetTextContent(blueprintName);
            }

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

        if (serviceElement.QuerySelector("LocoName Key") is {} locoNameId)
        {
            locoNameId.SetTextContent(Guid.NewGuid().ToString());
        }

        if (serviceElement.QuerySelector("LocoName English") is {} locoName)
        {
            locoName.SetTextContent(preload.LocomotiveName);
        }

        if (serviceElement.QuerySelector("LocoBP iBlueprintLibrary-cAbsoluteBlueprintID BlueprintID") is {} blueprintId)
        {
            blueprintId.SetTextContent(preload.BlueprintId);
        }

        if (serviceElement.QuerySelector("LocoBP iBlueprintLibrary-cAbsoluteBlueprintID BlueprintSetID iBlueprintLibrary-cBlueprintSetID Provider") is {} blueprintProviderId)
        {
            blueprintProviderId.SetTextContent(preload.BlueprintIdProvider);
        }

        if (serviceElement.QuerySelector("LocoBP iBlueprintLibrary-cAbsoluteBlueprintID BlueprintSetID iBlueprintLibrary-cBlueprintSetID Product") is {} blueprintProductId)
        {
            blueprintProductId.SetTextContent(preload.BlueprintIdProduct);
        }

        if (serviceElement.QuerySelector("LocoClass") is {} locoClass)
        {
            locoClass.SetTextContent(LocoClassUtils.ToLongFormString(preload.EngineType));
        }

        if (serviceElement.QuerySelector("LocoAuthor") is {} locoAuthor)
        {
            locoAuthor.SetTextContent(preload.BlueprintIdProvider);
        }

        if (serviceElement.QuerySelector("FilePath") is {} filePath)
        {
            var parts = preload.BlueprintId.Split('\\');
            var partsWithoutFilename = parts[..^1];
            var blueprintDirectory = string.Join('\\', partsWithoutFilename);
            var packagedPath = $@"{preload.BlueprintIdProvider}\{preload.BlueprintIdProduct}\{blueprintDirectory}";

            filePath.SetTextContent(packagedPath);
        }

        XmlException.ThrowIfDocumentInvalid(document);

        return document;
    }

    private static async Task WriteScenarioDocument(string path, IXmlDocument document)
    {
        const string filename = "Scenario.bin.xml";
        var destination = Path.Join(path, filename);

        File.Delete(filename);

        await document.ToXmlAsync(destination);

        File.Delete(filename.Replace(".bin.xml", ".bin"));

        await Serz.Convert(destination);
    }

    private static async Task WriteScenarioPropertiesDocument(string path, IXmlDocument document)
    {
        const string filename = "ScenarioProperties.xml";
        var destination = Path.Join(path, filename);

        File.Delete(filename);

        await document.ToXmlAsync(destination);
    }
}
