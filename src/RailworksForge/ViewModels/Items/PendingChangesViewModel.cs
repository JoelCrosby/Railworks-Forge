using System.ComponentModel;
using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;

using Echoes;

using RailworksForge.Core.Scenarios;

namespace RailworksForge.ViewModels;

public partial class PendingChangesViewModel(IScenarioEditor editor) : ObservableObject
{
    private bool _isAttached;

    [ObservableProperty]
    public partial bool HasChanges { get; private set; }

    [ObservableProperty]
    public partial string Summary { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string Details { get; private set; } = string.Empty;

    public void Attach()
    {
        if (_isAttached)
        {
            return;
        }

        _isAttached = true;
        editor.PropertyChanged += OnEditorChanged;
        TranslationProvider.OnCultureChanged += OnCultureChanged;

        Refresh();
    }

    public void Detach()
    {
        _isAttached = false;
        editor.PropertyChanged -= OnEditorChanged;
        TranslationProvider.OnCultureChanged -= OnCultureChanged;
    }

    private void OnEditorChanged(object? sender, PropertyChangedEventArgs e)
    {
        Refresh();
    }

    private void OnCultureChanged(object? sender, CultureInfo culture)
    {
        Refresh();
    }

    private void Refresh()
    {
        var changes = editor.PendingChanges ?? [];
        var hasChanges = editor.HasPendingChanges;

        HasChanges = hasChanges;
        Summary = hasChanges ? PendingChangesText.Summary(changes) : string.Empty;
        Details = changes.Count > 0 ? PendingChangesText.Details(changes) : string.Empty;
    }
}
