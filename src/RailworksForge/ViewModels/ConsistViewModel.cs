using System.Linq;
using System.Threading.Tasks;

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

    public async Task LoadImage()
    {
        ImageBitmap = await Task.Run(GetImageBitmap);
    }

    private Task<Bitmap?> GetImageBitmap()
    {
        var consist = Consist.Vehicles.FirstOrDefault();
        return BitmapUtils.GetImageBitmap(consist);
    }
}
