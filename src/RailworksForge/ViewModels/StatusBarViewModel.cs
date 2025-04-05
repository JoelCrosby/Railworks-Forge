using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core;

namespace RailworksForge.ViewModels;

public partial class StatusBarViewModel : ViewModelBase
{
    [ObservableProperty]
    private string? _statusText;

    [ObservableProperty]
    private float _progress;

    [ObservableProperty]
    private string? _progressText;

    [ObservableProperty]
    private bool _showProgress;

    public StatusBarViewModel()
    {
        StatusText = Paths.GetGameDirectory();
    }
}
