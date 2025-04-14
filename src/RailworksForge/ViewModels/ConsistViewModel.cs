using System.Linq;

using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core.Models;
using RailworksForge.Util;

namespace RailworksForge.ViewModels;

public partial class ConsistViewModel : ViewModelBase
{
    public Consist Consist { get; }

    [ObservableProperty]
    private Bitmap? _imageBitmap;

    public ConsistViewModel(Consist consist)
    {
        Consist = consist;
    }

    public void LoadImage()
    {
        ImageBitmap = GetImageBitmap();
    }

    private Bitmap? GetImageBitmap()
    {
        var consist = Consist.Vehicles.FirstOrDefault();
        return BitmapUtils.GetImageBitmap(consist);
    }
}
