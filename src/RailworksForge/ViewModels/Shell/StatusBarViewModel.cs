using CommunityToolkit.Mvvm.ComponentModel;

using Echoes;

using RailworksForge.Core.Config;
using RailworksForge.Translations;

namespace RailworksForge.ViewModels;

public partial class StatusBarViewModel : ObservableObject
{
    private readonly IGameDirectoryService _gameDirectory;

    [ObservableProperty]
    public partial string? StatusText { get; set; }

    public StatusBarViewModel(IGameDirectoryService gameDirectory)
    {
        _gameDirectory = gameDirectory;

        ShowGameDirectory();

        TranslationProvider.OnCultureChanged += (_, _) => ShowGameDirectory();
    }

    private void ShowGameDirectory()
    {
        StatusText = string.Format(Strings.railworks_directory.CurrentValue, _gameDirectory.Current);
    }
}
