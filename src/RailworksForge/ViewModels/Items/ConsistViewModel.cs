using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core.Models;

namespace RailworksForge.ViewModels;

public partial class ConsistViewModel(Consist consist) : ObservableObject
{
    public Consist Consist { get; } = consist;

    // Acquisition state is read from disk, and rows are built off the UI thread, so resolve it here.
    public RowHighlightState Highlight { get; } = GetHighlight(consist);

    [ObservableProperty]
    public partial Bitmap? ImageBitmap { get; set; }

    private static RowHighlightState GetHighlight(Consist consist)
    {
        return consist switch
        {
            { ConsistAcquisitionState: AcquisitionState.Missing } => RowHighlightState.Missing,
            { ConsistAcquisitionState: AcquisitionState.Partial } => RowHighlightState.Partial,
            { PlayerDriver: true } => RowHighlightState.Player,
            _ => RowHighlightState.None,
        };
    }
}
