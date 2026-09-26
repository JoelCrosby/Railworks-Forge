using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core.Models;

namespace RailworksForge.ViewModels;

public partial class PreloadConsistViewModel(PreloadConsist consist) : ObservableObject
{
    public PreloadConsist Consist { get; } = consist;

    [ObservableProperty]
    public partial Bitmap? ImageBitmap { get; set; }
}
