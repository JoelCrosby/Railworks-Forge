using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core;
using RailworksForge.Core.Models;
using RailworksForge.Core.Models.Common;
using RailworksForge.Util;
using RailworksForge.Translations;

namespace RailworksForge.ViewModels;

public partial class CheckAssetsViewModel(Route route, RouteAssetCheckService assetCheck) : DialogViewModel
{
    public Route Route { get; } = route;

    public RangeObservableCollection<Blueprint> Blueprints { get; } = [];

    [ObservableProperty]
    public partial int LoadingProgress { get; set; }

    [ObservableProperty]
    public partial string LoadingMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LoadingStatusMessage { get; set; } = string.Empty;

    protected override Task OnActivated()
    {
        var progress = new Progress<AssetCheckProgress>(ShowProgress);

        return Loading.RunAsync(
            Strings.checking_route_assets.CurrentValue,
            token => assetCheck.FindMissingAssets(Route, progress, token),
            Blueprints.ReplaceWith);
    }

    private void ShowProgress(AssetCheckProgress progress)
    {
        var format = progress.Stage switch
        {
            AssetCheckStage.ReadingFiles => Strings.processed_files,
            _ => Strings.checked_blueprints,
        };

        LoadingProgress = progress.Percentage;
        LoadingMessage = string.Format(format.CurrentValue, progress.Completed, progress.Total);
    }
}
