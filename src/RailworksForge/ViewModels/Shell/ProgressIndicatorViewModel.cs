using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core.Packaging;
using RailworksForge.Translations;

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
        ProgressMessage = GetFileProgressText(model);
        StatusMessage = GetStageText(model);
    }

    private static string GetFileProgressText(InstallProgress model)
    {
        var hasFileProgress = model.FileCount > 0;

        return hasFileProgress
            ? string.Format(Strings.processing_file.CurrentValue, model.FilesProcessed, model.FileCount)
            : string.Empty;
    }

    private static string GetStageText(InstallProgress model)
    {
        var format = model.Stage switch
        {
            InstallStage.Installing => Strings.installing_package,
            InstallStage.AlreadyInstalled => Strings.package_already_installed,
            InstallStage.Scanning => Strings.scanning_package_files,
            InstallStage.ClearingCache => Strings.clearing_pak_cache,
            _ => Strings.package_installed,
        };

        return string.Format(format.CurrentValue, model.PackageName);
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
