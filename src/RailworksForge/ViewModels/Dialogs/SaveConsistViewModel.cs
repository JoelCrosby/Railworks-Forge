using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using RailworksForge.Core.Models;

namespace RailworksForge.ViewModels;

public partial class SaveConsistViewModel : DialogViewModel<SavedConsist>
{
    public required string LocomotiveName { get; init; }

    public required string ConsistElement { get; init; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string? Name { get; set; }

    private bool HasName => !string.IsNullOrWhiteSpace(Name);

    [RelayCommand(CanExecute = nameof(HasName))]
    private void Save()
    {
        var savedConsist = new SavedConsist
        {
            Name = Name!,
            LocomotiveName = LocomotiveName,
            ConsistElement = ConsistElement,
        };

        Close(savedConsist);
    }
}
