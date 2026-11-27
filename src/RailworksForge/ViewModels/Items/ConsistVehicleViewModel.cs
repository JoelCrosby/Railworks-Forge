using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core.Assets;
using RailworksForge.Core.Consists;

namespace RailworksForge.ViewModels;

public partial class ConsistVehicleViewModel(ConsistRailVehicle vehicle, int index) : ObservableObject
{
    public ConsistRailVehicle Vehicle { get; } = vehicle;

    public int Index { get; } = index;

    public int Position => Index + 1;

    public bool IsMissing => Vehicle.AcquisitionState is AcquisitionState.Missing;

    [ObservableProperty]
    public partial Bitmap? ImageBitmap { get; set; }
}
