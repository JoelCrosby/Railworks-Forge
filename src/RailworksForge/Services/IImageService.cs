using Avalonia.Media.Imaging;

using RailworksForge.Core.Assets;
using RailworksForge.Core.Consists;
using RailworksForge.Core.Routes;

namespace RailworksForge.Services;

public interface IImageService
{
    Bitmap? GetRouteImage(Route route);

    Bitmap? GetBlueprintImage(Blueprint? blueprint);

    Bitmap? GetConsistImage(Consist consist);
}
