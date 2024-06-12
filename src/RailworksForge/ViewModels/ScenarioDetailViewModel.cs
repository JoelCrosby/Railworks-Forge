using System.Reactive;

using RailworksForge.Core.Models;
using RailworksForge.Util;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public class ScenarioDetailViewModel : ViewModelBase
{
    public Scenario Scenario { get; }

    public ReactiveCommand<Unit, Unit> OpenInExplorerCommand { get; }
    public ReactiveCommand<Unit, string> ExportBinXmlCommand { get; }
    public ReactiveCommand<Unit, string> ExportXmlBinCommand { get; }
    public ReactiveCommand<Unit, Unit> ClickedConsistCommand { get; }

    public Consist? SelectedConsist { get; set; }

    public ScenarioDetailViewModel(Scenario scenario)
    {
        Scenario = scenario;

        OpenInExplorerCommand = ReactiveCommand.Create(() =>
        {
            if (Scenario.Path is null) return;

            Launcher.Open(Scenario.Path);
        });

        ExportBinXmlCommand = ReactiveCommand.CreateFromTask(scenario.ConvertBinToXml);
        ExportXmlBinCommand = ReactiveCommand.CreateFromTask(scenario.ConvertXmlToBin);

        ClickedConsistCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedConsist is null) return;

            Utils.GetApplicationViewModel().SelectScenarioConsist(Scenario, SelectedConsist);
        });
    }
}
