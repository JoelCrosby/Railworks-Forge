using System;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using RailworksForge.Core;
using RailworksForge.Translations;

namespace RailworksForge.ViewModels;

public partial class VehicleIndexDialogViewModel(VehicleIndex index) : DialogViewModel
{
    [ObservableProperty]
    public partial bool IsDiscovering { get; set; } = true;

    [ObservableProperty]
    public partial double ProgressPercent { get; set; }

    [ObservableProperty]
    public partial string Status { get; set; } = Strings.vehicle_index_discovering.CurrentValue;

    [ObservableProperty]
    public partial string? FileProgress { get; set; }

    [ObservableProperty]
    public partial string? Summary { get; set; }

    protected override async Task OnActivated()
    {
        var progress = new Progress<VehicleIndexProgress>(value =>
        {

            if (IsActive && Loading.IsLoading)
            {
                ApplyProgress(value);
            }
        });
        await Loading.RunAsync(
            Strings.vehicle_indexing.CurrentValue,
            token => index.RefreshAsync(progress, token),
            ApplyProgress,
            allowRetry: false);
        IsDiscovering = false;
    }

    private void ApplyProgress(VehicleIndexProgress progress)
    {
        IsDiscovering = progress.Stage == VehicleIndexStage.Discovering;
        ProgressPercent = progress.TotalSources == 0 ? 0 : 100.0 * progress.Sources / progress.TotalSources;
        Status = progress.Stage switch
        {
            VehicleIndexStage.Discovering => Strings.vehicle_index_discovering.CurrentValue,
            VehicleIndexStage.Saving => Strings.vehicle_index_saving.CurrentValue,
            VehicleIndexStage.Completed => Strings.vehicle_index_complete.CurrentValue,
            _ => Strings.vehicle_indexing.CurrentValue,
        };
        FileProgress = string.Format(Strings.vehicle_index_files.CurrentValue, progress.Sources, progress.TotalSources);
        Summary = string.Format(
            Strings.vehicle_index_progress.CurrentValue,
            progress.Sources,
            progress.Vehicles,
            progress.Skipped);

        if (progress.Stage == VehicleIndexStage.Completed)
        {
            ProgressPercent = 100;
        }
    }

    [RelayCommand]
    private void CancelIndexing()
    {
        Loading.Cancel();
        IsDiscovering = false;
        Status = Strings.vehicle_index_cancelled.CurrentValue;
    }
}
