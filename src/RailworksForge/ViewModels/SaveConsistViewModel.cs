using System.Reactive;

using RailworksForge.Core.Models;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public class SaveConsistViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, SavedConsist?> SaveConsistCommand { get; }

    public required string LocomotiveName { get; init; }

    public required string ConsistElement { get; init; }

    public string? Name { get; set; }

    public SaveConsistViewModel()
    {
        SaveConsistCommand = ReactiveCommand.Create(() =>
        {
            if (Name is null || ConsistElement is null || LocomotiveName is null)
            {
                return null;
            }

            return new SavedConsist
            {
                Name = Name,
                LocomotiveName = LocomotiveName,
                ConsistElement = ConsistElement,
            };
        });
    }
}
