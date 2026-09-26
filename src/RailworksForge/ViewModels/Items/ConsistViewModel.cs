using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core.Models;

namespace RailworksForge.ViewModels;

public partial class ConsistViewModel(Consist consist) : ObservableObject
{
    public Consist Consist { get; } = consist;

    [ObservableProperty]
    public partial Bitmap? ImageBitmap { get; set; }
}
