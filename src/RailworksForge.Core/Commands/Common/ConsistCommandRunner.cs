using RailworksForge.Core.Models;

namespace RailworksForge.Core.Commands.Common;

public class ConsistCommandRunner
{
    public required Scenario Scenario { get; init; }

    public required List<IConsistCommand> Commands { get; init; }

    public async Task Run()
    {
        var scenarioPropertiesDocument = await Scenario.GetPropertiesXmlDocument();
        var scenarioDocument = await Scenario.GetXmlDocument(false);

        var context = new ConsistCommandContext
        {
            Scenario = Scenario,
            ScenarioDocument = scenarioDocument,
            ScenarioPropertiesDocument = scenarioPropertiesDocument,
        };

        foreach (var command in Commands)
        {
            await command.Run(context);
        }

        Scenario.CreateBackup();

        await ScenarioWriter.WriteBinary(context.Scenario, scenarioDocument);
        await ScenarioWriter.WritePropertiesDocument(context.Scenario, scenarioPropertiesDocument);

        Cache.ClearScenarioCache(Scenario);
    }
}
