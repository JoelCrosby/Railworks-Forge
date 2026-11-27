using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using RailworksForge.Core.Common;
using RailworksForge.Core.Routes;
using RailworksForge.Translations;

namespace RailworksForge.ViewModels;

public partial class EditRouteDetailsViewModel : DialogViewModel<RouteDetails>
{
    private readonly RouteDetails _original;

    public EditRouteDetailsViewModel(string routeId, RouteDetails details)
    {
        _original = details;
        RouteId = routeId;
        Edited = details;
        SelectedLanguage = details.Name.FirstLanguageWithText ?? GameLanguage.English;
        ShowLanguage(SelectedLanguage);
    }

    public LanguageChoices Languages { get; } = new();

    public string RouteId { get; }

    public string AuthoredLanguage => _original.AuthoredLanguage;

    public bool HasAuthoredLanguage => AuthoredLanguage.Length > 0;

    public bool IsLocked => _original.IsLocked;

    public bool IsWorkshopItem => _original.IsWorkshopItem;

    [ObservableProperty]
    public partial RouteDetails Edited { get; private set; }

    [ObservableProperty]
    public partial GameLanguage SelectedLanguage { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    public string? NameFallback
    {
        get
        {
            var isEmptyHere = !Edited.Name.HasText(SelectedLanguage);
            var showsAnotherLanguage = isEmptyHere && Edited.Name.HasAnyText;

            return showsAnotherLanguage
                ? string.Format(Strings.details_language_fallback.CurrentValue, Edited.Name.DisplayText)
                : null;
        }
    }

    public bool HasName => Edited.Name.HasAnyText;

    public bool HasChanges => Edited != _original;

    private bool CanSave => HasChanges && HasName;

    [RelayCommand]
    private void SelectLanguage(GameLanguage language)
    {
        SelectedLanguage = language;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        Close(Edited);
    }

    partial void OnSelectedLanguageChanged(GameLanguage value)
    {
        ShowLanguage(value);
    }

    partial void OnNameChanged(string value)
    {
        Edited = Edited with { Name = Edited.Name.With(SelectedLanguage, value) };
    }

    partial void OnEditedChanged(RouteDetails value)
    {
        Languages.MarkText(value.Name.HasText);
        OnPropertyChanged(nameof(NameFallback));
        OnPropertyChanged(nameof(HasName));
        SaveCommand.NotifyCanExecuteChanged();
    }

    private void ShowLanguage(GameLanguage language)
    {
        Languages.Select(language);
        Name = Edited.Name[language];
        OnPropertyChanged(nameof(NameFallback));
    }
}
