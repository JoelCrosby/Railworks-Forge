using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core;

namespace RailworksForge.ViewModels;

public partial class StatusBarViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string? StatusText { get; set; }

    [ObservableProperty]
    public partial float Progress { get; set; }

    [ObservableProperty]
    public partial string? ProgressText { get; set; }

    [ObservableProperty]
    public partial bool ShowProgress { get; set; }

    public StatusBarViewModel()
    {
        StatusText = $"Railworks Directory - {Paths.GetGameDirectory()}";
    }
}
