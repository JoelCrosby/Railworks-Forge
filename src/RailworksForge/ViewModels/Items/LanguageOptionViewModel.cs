using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core.Common;

namespace RailworksForge.ViewModels;

public partial class LanguageOptionViewModel(GameLanguage language, string label) : ObservableObject
{
    public GameLanguage Language { get; } = language;

    public string Label { get; } = label;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial bool HasText { get; set; }
}
