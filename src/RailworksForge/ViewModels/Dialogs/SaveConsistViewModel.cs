using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RailworksForge.ViewModels;

public partial class SaveConsistViewModel : DialogViewModel<string>
{
    public required string LocomotiveName { get; init; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string? Name { get; set; }

    private bool HasName => !string.IsNullOrWhiteSpace(Name);

    [RelayCommand(CanExecute = nameof(HasName))]
    private void Save()
    {
        Close(Name!);
    }
}
