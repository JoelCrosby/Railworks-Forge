using CommunityToolkit.Mvvm.ComponentModel;

using Echoes;

using RailworksForge.Core;
using RailworksForge.Translations;

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
        ShowGameDirectory();

        TranslationProvider.OnCultureChanged += (_, _) => ShowGameDirectory();
    }

    private void ShowGameDirectory()
    {
        StatusText = string.Format(Strings.railworks_directory.CurrentValue, Paths.GetGameDirectory());
    }
}
