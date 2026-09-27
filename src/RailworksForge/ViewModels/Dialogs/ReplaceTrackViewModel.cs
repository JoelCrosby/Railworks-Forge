using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using RailworksForge.Core;
using RailworksForge.Core.Models;
using RailworksForge.Translations;

namespace RailworksForge.ViewModels;

public partial class ReplaceTrackViewModel : DialogViewModel<ReplaceTracksRequest>
{
    private readonly TrackService _tracks;

    [ObservableProperty]
    public partial List<SelectTrackViewModel> RouteTracks { get; set; } = [];

    public ReplaceTrackViewModel(Route route, TrackService tracks)
    {
        Route = route;
        _tracks = tracks;

        Loading.PropertyChanged += (_, _) => ReplaceTracksCommand.NotifyCanExecuteChanged();
    }

    public Route Route { get; }

    private bool CanReplaceTracks
    {
        get
        {
            var isReady = !Loading.IsLoading && !Loading.HasError;
            var tracksReady = RouteTracks.All(track => !track.Loading.IsLoading && !track.Loading.HasError);
            var hasSelection = RouteTracks.Any(track => track.SelectedTrack is not null);

            return isReady && tracksReady && hasSelection;
        }
    }

    protected override Task OnActivated()
    {
        return Loading.RunAsync(Strings.loading_route_tracks.CurrentValue, _ => GetRouteTracks(), tracks => RouteTracks = tracks);
    }

    protected override void OnDeactivated()
    {
        foreach (var track in RouteTracks)
        {
            track.Deactivate();
        }
    }

    partial void OnRouteTracksChanged(List<SelectTrackViewModel> value)
    {
        foreach (var track in value)
        {
            track.PropertyChanged += (_, _) => ReplaceTracksCommand.NotifyCanExecuteChanged();
            track.Loading.PropertyChanged += (_, _) => ReplaceTracksCommand.NotifyCanExecuteChanged();
        }

        ReplaceTracksCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanReplaceTracks))]
    private void ReplaceTracks()
    {
        var request = new ReplaceTracksRequest
        {
            Replacements = RouteTracks.ConvertAll(track => new TrackReplacement
            {
                Blueprint = track.RouteBlueprint,
                ReplacementBlueprint = track.SelectedTrack?.Blueprint,
            }),
        };

        Close(request);
    }

    private async Task<List<SelectTrackViewModel>> GetRouteTracks()
    {
        var blueprints = await Route.GetTrackBlueprints();
        var providers = _tracks.GetProviders().ConvertAll(directory => new DirectoryItem(directory.Name, directory));

        return blueprints
            .Select(blueprint => new SelectTrackViewModel(_tracks)
            {
                Providers = providers,
                RouteBlueprint = blueprint.Blueprint,
                TrackCount = blueprint.Count,
            })
            .ToList();
    }
}
