using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core.Packaging;

namespace RailworksForge.ViewModels;

public partial class ProgressIndicatorViewModel : ObservableObject
{
    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial int Progress { get; set; }

    [ObservableProperty]
    public partial string ProgressMessage { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsVisible { get; set; }

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
