using Avalonia.ReactiveUI;

using RailworksForge.ViewModels;

namespace RailworksForge.Views.Dialogs;

public partial class ReplaceTrackDialog : ReactiveWindow<ReplaceTrackViewModel>
{
    public ReplaceTrackDialog()
    {
        InitializeComponent();
    }
}
