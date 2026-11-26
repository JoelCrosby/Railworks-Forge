using RailworksForge.Core.Routes;

namespace RailworksForge.Core.Scenarios;

public interface IScenarioService
{
    event Action? PlayerInfoUpdated;

    void RefreshPlayerInfo(IEnumerable<Scenario> scenarios);

    Task<string> ExportBinToXml(Scenario scenario);

    Task<string> ConvertXmlToBin(Scenario scenario);

    void ExtractScenarios(Route route);

    List<Scenario> GetScenarios(Route route, CancellationToken cancellationToken = default);

    IScenarioEditor CreateEditor(Scenario scenario);
}
