using System.ComponentModel;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Services;

namespace RailworksForge.ViewModels;

public partial class StatusBarViewModel : ObservableObject
{
    private readonly INavigationService _navigation;
    private ViewModelBase? _trackedPage;

    [ObservableProperty]
    public partial LoadingOperation? BackgroundTask { get; set; }

    public StatusBarViewModel(INavigationService navigation)
    {
        _navigation = navigation;

        navigation.PropertyChanged += OnNavigationChanged;
    }

    public string? StatusText => _trackedPage?.StatusText;

    private void OnNavigationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(INavigationService.CurrentPage))
        {
            return;
        }

        if (_trackedPage is not null)
        {
            _trackedPage.PropertyChanged -= OnPageChanged;
        }

        _trackedPage = _navigation.CurrentPage;

        if (_trackedPage is not null)
        {
            _trackedPage.PropertyChanged += OnPageChanged;
        }

        OnPropertyChanged(nameof(StatusText));
    }

    private void OnPageChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModelBase.StatusText))
        {
            OnPropertyChanged(nameof(StatusText));
        }
    }
}
