using System.Linq;

using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core.Models;
using RailworksForge.Util;

namespace RailworksForge.ViewModels;

public partial class ConsistViewModel(Consist consist) : ViewModelBase
{
    public Consist Consist { get; } = consist;

    [ObservableProperty]
    private Bitmap? _imageBitmap;

    public void LoadImage()
    {
        ImageBitmap = GetImageBitmap();
    }

    private Bitmap? GetImageBitmap()
    {
        var consist = Consist.LeadVehicle?.Blueprint;

        if (BitmapUtils.GetImageBitmap(consist) is { } image)
        {
            return image;
        }

        var last = Consist.Vehicles.LastOrDefault()?.Blueprint;
        return BitmapUtils.GetImageBitmap(last);
    }
}
