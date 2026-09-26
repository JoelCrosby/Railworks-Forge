using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core;
using RailworksForge.Core.Models;
using RailworksForge.Core.Models.Common;
using RailworksForge.Util;

namespace RailworksForge.ViewModels;

public partial class CheckAssetsViewModel(Route route, RouteAssetCheckService assetCheck) : DialogViewModel
{
    public Route Route { get; } = route;

    public ObservableCollection<Blueprint> Blueprints { get; } = [];

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
            "Checking route assets…",
            token => assetCheck.FindMissingAssets(Route, progress, token),
            Blueprints.ReplaceWith);
    }

    private void ShowProgress(AssetCheckProgress progress)
    {
        LoadingProgress = progress.Percentage;
        LoadingMessage = progress.Message;
    }
}
