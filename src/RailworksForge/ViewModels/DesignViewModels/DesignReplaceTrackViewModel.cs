using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

using RailworksForge.Core.Models.Common;
using RailworksForge.Core.Models.Examples;

namespace RailworksForge.ViewModels;

public class DesignReplaceTrackViewModel : ReplaceTrackViewModel
{
    private readonly List<Blueprint> _tracks =
    [
        new Blueprint
        {
            BlueprintSetIdProvider = "DTG",
            BlueprintSetIdProduct = "HamburgLubeck",
            BlueprintId = @"RailNetwork\Track\HL_Track_Concrete01.xml",
        },

        new Blueprint
        {
            BlueprintSetIdProvider = "DTG",
            BlueprintSetIdProduct = "HamburgLubeck",
            BlueprintId = @"RailNetwork\Track\HL_Track_Concrete02.xml",
        },

        new Blueprint
        {
            BlueprintSetIdProvider = "DTG",
            BlueprintSetIdProduct = "HamburgLubeck",
            BlueprintId = @"RailNetwork\Track\HL_Track_ConcreteNW.xml",
        },

        new Blueprint
        {
            BlueprintSetIdProvider = "DTG",
            BlueprintSetIdProduct = "HamburgLubeck",
            BlueprintId = @"RailNetwork\Track\Inv_Road.xml",
        },

    ];

    public DesignReplaceTrackViewModel() : base(Example.Route)
    {
        var models =_tracks.Select(track => new SelectTrackViewModel { RouteBlueprint = track});

        RouteTracks = Observable.Return(models.ToList());
        IsLoading = false;
    }
}
