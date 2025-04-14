using System.Linq;
using System.Threading.Tasks;

using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core.Models;
using RailworksForge.Util;

namespace RailworksForge.ViewModels;

public partial class PreloadConsistViewModel : ViewModelBase
{
    public PreloadConsist Consist { get; }

    [ObservableProperty]
    private Bitmap? _imageBitmap;

    public PreloadConsistViewModel(PreloadConsist consist)
    {
        Consist = consist;
    }

    public async Task LoadImage()
    {
        ImageBitmap = await Task.Run(GetImageBitmap);
    }

    private Bitmap? GetImageBitmap()
    {
        var consist = Consist.ConsistEntries.FirstOrDefault();
        return BitmapUtils.GetImageBitmap(consist?.Blueprint);
    }
}
