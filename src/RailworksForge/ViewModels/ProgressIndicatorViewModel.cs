using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core.Packaging;

namespace RailworksForge.ViewModels;

public partial class ProgressIndicatorViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _progress;

    [ObservableProperty]
    private string _progressMessage;

    [ObservableProperty]
    private string _statusMessage;

    [ObservableProperty]
    private bool _isVisible;

    public ProgressIndicatorViewModel()
    {
        ProgressMessage = string.Empty;
        StatusMessage = string.Empty;
    }

    public void UpdateProgress(InstallProgress model)
    {
        IsVisible = true;
        IsLoading = model.IsLoading;
        Progress = model.Progress;
        ProgressMessage = model.Message;
        StatusMessage = model.CurrentTask;
    }

    public void ClearProgress()
    {
        IsLoading = false;
        IsVisible = false;
        Progress = 0;
        StatusMessage = string.Empty;
        ProgressMessage = string.Empty;
    }
}
