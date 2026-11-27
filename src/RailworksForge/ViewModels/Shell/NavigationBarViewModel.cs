using System;
using System.ComponentModel;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using RailworksForge.Core.Scenarios;
using RailworksForge.Services;
using RailworksForge.Translations;

namespace RailworksForge.ViewModels;

public partial class NavigationBarViewModel : ObservableObject
{
    private readonly IDialogService _dialogs;
    private IScenarioEditor? _trackedEditor;

    public NavigationBarViewModel(INavigationService navigation, ToolsMenuViewModel tools, IDialogService dialogs)
    {
        Navigation = navigation;
        Tools = tools;
        _dialogs = dialogs;

        navigation.PropertyChanged += (_, _) => OnNavigationChanged();
    }

    public INavigationService Navigation { get; }

    public ToolsMenuViewModel Tools { get; }

    public bool IsSettingsActive => Navigation.IsSettingsActive;

    public bool ShowsTrail => !Navigation.IsSettingsActive;

    public bool IsRoutesCrumbCurrent => Navigation.CurrentRoute is null && !Navigation.IsSettingsActive;

    public bool IsRouteCrumbCurrent => Navigation.CurrentScenario is null && !Navigation.IsSettingsActive;

    public bool IsScenarioCrumbCurrent => Navigation.CurrentConsist is null && !Navigation.IsSettingsActive;

    public bool IsConsistCrumbCurrent => !Navigation.IsSettingsActive;

    public bool HasRoute => Navigation.CurrentRoute is not null;

    public bool HasScenario => Navigation.CurrentScenario is not null;

    public bool HasConsist => Navigation.CurrentConsist is not null;

    public bool IsScenarioDirty => Navigation.CurrentEditor is { HasPendingChanges: true };

    public bool CanGoBack => Navigation.IsSettingsActive || Navigation.CurrentRoute is not null;

    private void OnNavigationChanged()
    {
        TrackEditor(Navigation.CurrentEditor);

        OnPropertyChanged(nameof(IsSettingsActive));
        OnPropertyChanged(nameof(ShowsTrail));
        OnPropertyChanged(nameof(IsRoutesCrumbCurrent));
        OnPropertyChanged(nameof(IsRouteCrumbCurrent));
        OnPropertyChanged(nameof(IsScenarioCrumbCurrent));
        OnPropertyChanged(nameof(IsConsistCrumbCurrent));
        OnPropertyChanged(nameof(HasRoute));
        OnPropertyChanged(nameof(HasScenario));
        OnPropertyChanged(nameof(HasConsist));
        OnPropertyChanged(nameof(IsScenarioDirty));
        OnPropertyChanged(nameof(CanGoBack));
        GoBackCommand.NotifyCanExecuteChanged();
    }

    private void TrackEditor(IScenarioEditor? editor)
    {
        if (ReferenceEquals(_trackedEditor, editor))
        {
            return;
        }

        if (_trackedEditor is not null)
        {
            _trackedEditor.PropertyChanged -= OnEditorChanged;
        }

        _trackedEditor = editor;

        if (editor is not null)
        {
            editor.PropertyChanged += OnEditorChanged;
        }
    }

    private void OnEditorChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsScenarioDirty));
    }

    // Back steps up one level of the trail, or out of Settings to the page it was opened from.
    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private Task GoBack()
    {
        if (Navigation.IsSettingsActive)
        {
            Navigation.CloseSettings();

            return Task.CompletedTask;
        }

        if (Navigation.CurrentConsist is not null)
        {
            return Show(Navigation.ShowCurrentScenario);
        }

        if (Navigation.CurrentScenario is not null)
        {
            return DiscardEditsThen(Navigation.ShowCurrentRoute);
        }

        if (Navigation.CurrentRoute is null)
        {
            return Task.CompletedTask;
        }

        return DiscardEditsThen(Navigation.ShowRoutes);
    }

    [RelayCommand]
    private Task ShowRoutes()
    {
        return DiscardEditsThen(Navigation.ShowRoutes);
    }

    [RelayCommand]
    private Task ShowRoute()
    {
        return DiscardEditsThen(Navigation.ShowCurrentRoute);
    }

    // Showing the current page again reloads it from disk, so only that discards pending edits.
    [RelayCommand]
    private Task ShowScenario()
    {
        return Navigation.CurrentPage is ScenarioDetailViewModel
            ? DiscardEditsThen(Navigation.ShowCurrentScenario)
            : Show(Navigation.ShowCurrentScenario);
    }

    [RelayCommand]
    private Task ShowConsist()
    {
        return Navigation.CurrentPage is ConsistDetailViewModel
            ? DiscardEditsThen(Navigation.ShowCurrentConsist)
            : Show(Navigation.ShowCurrentConsist);
    }

    [RelayCommand]
    private void ShowSettings()
    {
        if (Navigation.IsSettingsActive)
        {
            Navigation.CloseSettings();

            return;
        }

        Navigation.ShowSettings();
    }

    private static Task Show(Action navigate)
    {
        navigate();

        return Task.CompletedTask;
    }

    private async Task DiscardEditsThen(Action navigate)
    {
        if (Navigation.CurrentEditor is { HasPendingChanges: true } editor)
        {
            var confirmation = new ConfirmationDialogViewModel
            {
                Title = Strings.scenario_unsaved_changes.CurrentValue,
                BodyText = Strings.scenario_unsaved_changes_body.CurrentValue,
                AcceptLabel = Strings.discard_changes.CurrentValue,
            };

            if (!await _dialogs.Show(confirmation))
            {
                return;
            }

            editor.Discard();
        }

        navigate();
    }
}
