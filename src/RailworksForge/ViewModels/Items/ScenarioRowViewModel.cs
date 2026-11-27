using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core.Scenarios;

namespace RailworksForge.ViewModels;

public partial class ScenarioRowViewModel(Scenario scenario) : ObservableObject
{
    public Scenario Scenario { get; } = scenario;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCompleted))]
    public partial ScenarioPlayerInfo PlayerInfo { get; set; } = scenario.PlayerInfo;

    public bool IsCompleted => PlayerInfo.Completion is "CompletedSuccessfully";

    [ObservableProperty]
    public partial Bitmap? LocomotiveImage { get; set; }

    public void RefreshPlayerInfo()
    {
        PlayerInfo = Scenario.PlayerInfo;
    }
}
