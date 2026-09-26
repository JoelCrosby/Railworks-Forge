using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RailworksForge.ViewModels;

public partial class ConfirmationDialogViewModel : DialogViewModel<bool>
{
    [ObservableProperty]
    public partial string? Title { get; set; }

    [ObservableProperty]
    public partial string? BodyText { get; set; }

    [ObservableProperty]
    public partial string? AcceptLabel { get; set; }

    [RelayCommand]
    private void Accept()
    {
        Close(true);
    }
}
