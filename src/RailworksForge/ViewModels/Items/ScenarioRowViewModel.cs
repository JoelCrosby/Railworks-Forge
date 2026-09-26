using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core.Models;

namespace RailworksForge.ViewModels;

public partial class ScenarioRowViewModel(Scenario scenario) : ObservableObject
{
    public Scenario Scenario { get; } = scenario;

    [ObservableProperty]
    public partial ScenarioPlayerInfo PlayerInfo { get; set; } = scenario.PlayerInfo;

    [ObservableProperty]
    public partial Bitmap? LocomotiveImage { get; set; }

    public void RefreshPlayerInfo()
    {
        PlayerInfo = Scenario.PlayerInfo;
    }
}
