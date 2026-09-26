using System.Collections.Generic;
using System.Linq;

using RailworksForge.Core;
using RailworksForge.Core.Models.Common;
using RailworksForge.Services;
using RailworksForge.ViewModels;

namespace RailworksForge.DesignData;

public static class DesignViewModels
{
    public static NavigationBarViewModel NavigationBar
    {
        get
        {
            var navigation = DesignServices.Get<NavigationService>();

            navigation.CurrentRoute = new RouteViewModel(DesignData.Route);
            navigation.CurrentScenario = DesignData.Scenario;

            return DesignServices.Get<NavigationBarViewModel>();
        }
    }

    public static ToolbarViewModel Toolbar => DesignServices.Get<ToolbarViewModel>();

    public static StatusBarViewModel StatusBar => new()
    {
        StatusText = "84 Routes found",
        ShowProgress = true,
        Progress = 0.42f,
        ProgressText = "Extracting scenario .ap file to .xml",
    };

    public static ProgressIndicatorViewModel ProgressIndicator => new()
    {
        IsLoading = true,
        Progress = 34,
        ProgressMessage = "Loading...",
        StatusMessage = "Installing package 2 of 8...",
    };

    public static RouteDetailViewModel RouteDetail
    {
        get
        {
            var route = new RouteViewModel(DesignData.Route);
            var viewModel = DesignServices.Create<RouteDetailViewModel>(route);

            viewModel.Scenarios.Reset(DesignData.Scenarios.ConvertAll(scenario => new ScenarioRowViewModel(scenario)));

            return viewModel;
        }
    }

    public static ScenarioDetailViewModel ScenarioDetail => DesignServices.Create<ScenarioDetailViewModel>(DesignData.Scenario);

    public static SettingsViewModel Settings => DesignServices.Get<SettingsViewModel>();

    public static ConfirmationDialogViewModel ConfirmationDialog => new()
    {
        Title = "Replace Consist",
        BodyText = "Are you sure you want to replace the selected consist(s).",
        AcceptLabel = "Replace Consist",
    };

    public static SaveConsistViewModel SaveConsist => new()
    {
        Name = "Class 390 'Pendolino'",
        LocomotiveName = "Class 390 'Pendolino'",
        ConsistElement = string.Empty,
    };

    public static ReplaceConsistViewModel ReplaceConsist => DesignServices.Create<ReplaceConsistViewModel>();

    public static CheckAssetsViewModel CheckAssets
    {
        get
        {
            var viewModel = DesignServices.Create<CheckAssetsViewModel>(DesignData.Route);

            viewModel.LoadingMessage = "Processing 24 of 100 files ( %24 )";
            viewModel.LoadingStatusMessage = "Processing file: /cache/file/path/binary.bin";
            viewModel.LoadingProgress = 24;

            return viewModel;
        }
    }

    public static ReplaceTrackViewModel ReplaceTrack
    {
        get
        {
            var tracks = DesignServices.Get<TrackService>();
            var viewModel = DesignServices.Create<ReplaceTrackViewModel>(DesignData.Route);

            viewModel.RouteTracks = TrackBlueprints
                .Select(blueprint => new SelectTrackViewModel(tracks) { RouteBlueprint = blueprint, TrackCount = 512 })
                .ToList();

            return viewModel;
        }
    }

    private static List<Blueprint> TrackBlueprints =>
    [
        CreateTrackBlueprint(@"RailNetwork\Track\HL_Track_Concrete01.xml"),
        CreateTrackBlueprint(@"RailNetwork\Track\HL_Track_Concrete02.xml"),
        CreateTrackBlueprint(@"RailNetwork\Track\HL_Track_ConcreteNW.xml"),
        CreateTrackBlueprint(@"RailNetwork\Track\Inv_Road.xml"),
    ];

    private static Blueprint CreateTrackBlueprint(string blueprintId)
    {
        return new Blueprint
        {
            BlueprintSetIdProvider = "DTG",
            BlueprintSetIdProduct = "HamburgLubeck",
            BlueprintId = blueprintId,
        };
    }
}
