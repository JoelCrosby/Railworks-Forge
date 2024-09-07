using System.Collections.Generic;
using System.Linq;

using RailworksForge.Core.Models.Common;

namespace RailworksForge.ViewModels;

public class DesignReplaceTrackViewModel : ReplaceTrackViewModel
{
    private readonly List<Blueprint> _tracks =
    [
        new()
        {
            BlueprintSetIdProvider = "DTG",
            BlueprintSetIdProduct = "HamburgLubeck",
            BlueprintId = @"RailNetwork\Track\HL_Track_Concrete01.xml",
        },

        new()
        {
            BlueprintSetIdProvider = "DTG",
            BlueprintSetIdProduct = "HamburgLubeck",
            BlueprintId = @"RailNetwork\Track\HL_Track_Concrete02.xml",
        },

        new()
        {
            BlueprintSetIdProvider = "DTG",
            BlueprintSetIdProduct = "HamburgLubeck",
            BlueprintId = @"RailNetwork\Track\HL_Track_ConcreteNW.xml",
        },

        new()
        {
            BlueprintSetIdProvider = "DTG",
            BlueprintSetIdProduct = "HamburgLubeck",
            BlueprintId = @"RailNetwork\Track\Inv_Road.xml",
        },

    ];

    public DesignReplaceTrackViewModel() : base(DesignData.DesignData.Route)
    {
        var models =_tracks.Select(track => new SelectTrackViewModel { RouteBlueprint = track});

        RouteTracks = models.ToList();
        IsLoading = false;
    }
}
