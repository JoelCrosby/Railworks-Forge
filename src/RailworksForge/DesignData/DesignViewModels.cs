using System;
using System.Collections.Generic;
using System.Linq;

using RailworksForge.Core.Assets;
using RailworksForge.Core.Common;
using RailworksForge.Core.Routes;
using RailworksForge.Core.Scenarios;
using RailworksForge.Services;
using RailworksForge.ViewModels;

namespace RailworksForge.DesignData;

public static class DesignViewModels
{
    public static NavigationBarViewModel NavigationBar
    {
        get
        {
            var navigation = (NavigationService)DesignServices.Get<INavigationService>();

            navigation.CurrentRoute = new RouteViewModel(DesignData.Route);
            navigation.CurrentScenario = DesignData.Scenario;

            return DesignServices.Get<NavigationBarViewModel>();
        }
    }

    public static StatusBarViewModel StatusBar
    {
        get
        {
            var statusBar = DesignServices.Create<StatusBarViewModel>();

            statusBar.BackgroundTask = new LoadingOperation { IsLoading = true, Message = "Loading scenario information" };

            return statusBar;
        }
    }

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
    };

    public static EditScenarioDetailsViewModel EditScenarioDetails => new(new ScenarioDetails
    {
        Name = LocalisedText.From(
            (GameLanguage.English, "[DWA] IR 1831: Dej Calatori - Beclean pe Somes"),
            (GameLanguage.German, "[DWA] IR 1831: Dej Calatori - Beclean pe Someș")),
        Description = LocalisedText.From((GameLanguage.English, "<supply description>")),
        Briefing = LocalisedText.From((GameLanguage.English, "<supply briefing>")),
        StartLocation = LocalisedText.From((GameLanguage.English, "Dej Calatori")),
        DurationMinutes = 50,
        Rating = 4,
        Season = ScenarioSeason.Summer,
        StartDate = new DateOnly(2010, 7, 1),
        StartTime = new TimeSpan(14, 14, 0),
        ScenarioClass = ScenarioClass.Standard,
    });

    public static EditRouteDetailsViewModel EditRouteDetails => new("fc8df6e1-dc30-4efc-92a6-2fe065b39826", new RouteDetails
    {
        Name = LocalisedText.From((GameLanguage.English, "Tirol Austria"), (GameLanguage.German, "Tirol Österreich")),
        AuthoredLanguage = "en",
        IsLocked = true,
    });

    public static ReplaceConsistViewModel ReplaceConsist => DesignServices.Create<ReplaceConsistViewModel>();

    public static CheckAssetsViewModel CheckAssets
    {
        get
        {
            var viewModel = DesignServices.Create<CheckAssetsViewModel>(DesignData.Route);

            viewModel.LoadingMessage = "Processing 24 of 100 files ( %24 )";
            viewModel.LoadingProgress = 24;

            return viewModel;
        }
    }

    public static ReplaceTrackViewModel ReplaceTrack
    {
        get
        {
            var tracks = DesignServices.Get<ITrackService>();
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
