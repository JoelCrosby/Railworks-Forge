using RailworksForge.Core.Models;

namespace RailworksForge.Core.Commands.Common;

public class ConsistCommandRunner
{
    public required Scenario Scenario { get; init; }

    public required List<IConsistCommand> Commands { get; init; }

    public async Task Run()
    {
        using var scenarioLock = await ScenarioLocks.Acquire(Scenario);

        var context = await LoadContext(Scenario, useCache: false);

        foreach (var command in Commands)
        {
            await command.Run(context);
        }

        await Write(context);
    }

    public static async Task<ConsistCommandContext> LoadContext(Scenario scenario, bool useCache)
    {
        var scenarioPropertiesDocument = await scenario.GetPropertiesXmlDocument();
        var scenarioDocument = await scenario.GetXmlDocument(useCache);

        return new ConsistCommandContext
        {
            Scenario = scenario,
            ScenarioDocument = scenarioDocument,
            ScenarioPropertiesDocument = scenarioPropertiesDocument,
        };
    }

    public static async Task Write(ConsistCommandContext context)
    {
        context.Scenario.CreateBackup();

        await ScenarioWriter.WriteBinary(context.Scenario, context.ScenarioDocument);
        await ScenarioWriter.WritePropertiesDocument(context.Scenario, context.ScenarioPropertiesDocument);

        Cache.ClearScenarioCache(context.Scenario);
    }
}
