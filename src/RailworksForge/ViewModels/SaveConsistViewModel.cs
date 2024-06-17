using System.Reactive;

using RailworksForge.Core.Models;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public class SaveConsistViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, SaveConsistViewModel> SaveConsistCommand { get; }

    public required Consist Consist { get; init; }

    public string? Name { get; set; }

    public SaveConsistViewModel()
    {
        SaveConsistCommand = ReactiveCommand.Create(() => this);
    }
}
