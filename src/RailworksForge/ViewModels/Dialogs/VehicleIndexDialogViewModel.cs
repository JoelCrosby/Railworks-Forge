using System;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using RailworksForge.Core.Vehicles;
using RailworksForge.Translations;

namespace RailworksForge.ViewModels;

public partial class VehicleIndexDialogViewModel(IVehicleIndex index) : DialogViewModel
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
            var isIndexing = IsActive && Loading.IsLoading;

            if (isIndexing)
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
        var isComplete = progress.Stage == VehicleIndexStage.Completed;
        var hasSources = progress.TotalSources > 0;
        var sourcePercent = hasSources ? 100.0 * progress.Sources / progress.TotalSources : 0;

        IsDiscovering = progress.Stage == VehicleIndexStage.Discovering;
        ProgressPercent = isComplete ? 100 : sourcePercent;
        Status = GetStatus(progress.Stage);
        FileProgress = string.Format(Strings.vehicle_index_files.CurrentValue, progress.Sources, progress.TotalSources);
        Summary = string.Format(
            Strings.vehicle_index_progress.CurrentValue,
            progress.Sources,
            progress.Vehicles,
            progress.Skipped);
    }

    private static string GetStatus(VehicleIndexStage stage)
    {
        var status = stage switch
        {
            VehicleIndexStage.Discovering => Strings.vehicle_index_discovering,
            VehicleIndexStage.Saving => Strings.vehicle_index_saving,
            VehicleIndexStage.Completed => Strings.vehicle_index_complete,
            _ => Strings.vehicle_indexing,
        };

        return status.CurrentValue;
    }

    [RelayCommand]
    private void CancelIndexing()
    {
        Loading.Cancel();
        IsDiscovering = false;
        Status = Strings.vehicle_index_cancelled.CurrentValue;
    }
}
