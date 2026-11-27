using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core.Consists;
using RailworksForge.Translations;

namespace RailworksForge.ViewModels;

public partial class ConsistVehicleViewModel(ConsistRailVehicle vehicle, int position) : ObservableObject
{
    public ConsistRailVehicle Vehicle { get; } = vehicle;

    public string Position { get; } = position.ToString();

    public string Caption => Vehicle.Flipped ? Strings.flipped.CurrentValue : Strings.forward.CurrentValue;

    [ObservableProperty]
    public partial Bitmap? ImageBitmap { get; set; }

    public void RefreshCaption()
    {
        OnPropertyChanged(nameof(Caption));
    }
}
