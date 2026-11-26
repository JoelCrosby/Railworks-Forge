using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using RailworksForge.Core.Routes;
using RailworksForge.Translations;

namespace RailworksForge.ViewModels;

public partial class ReplaceTrackViewModel : DialogViewModel<ReplaceTracksRequest>
{
    private readonly ITrackService _tracks;

    [ObservableProperty]
    public partial List<SelectTrackViewModel> RouteTracks { get; set; } = [];

    public ReplaceTrackViewModel(Route route, ITrackService tracks)
    {
        Route = route;
        _tracks = tracks;

        Loading.PropertyChanged += OnReplaceStateChanged;
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

    partial void OnRouteTracksChanged(List<SelectTrackViewModel> oldValue, List<SelectTrackViewModel> newValue)
    {
        foreach (var track in oldValue)
        {
            track.PropertyChanged -= OnReplaceStateChanged;
            track.Loading.PropertyChanged -= OnReplaceStateChanged;
            track.Deactivate();
        }

        foreach (var track in newValue)
        {
            track.PropertyChanged += OnReplaceStateChanged;
            track.Loading.PropertyChanged += OnReplaceStateChanged;
        }

        ReplaceTracksCommand.NotifyCanExecuteChanged();
    }

    private void OnReplaceStateChanged(object? sender, PropertyChangedEventArgs e)
    {
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
        var blueprints = await _tracks.GetRouteTrackBlueprints(Route);
        var providers = _tracks.GetProviders();

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
