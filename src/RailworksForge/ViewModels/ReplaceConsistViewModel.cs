using System.Collections.Generic;
using System.Reactive;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core.Models;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public partial class ReplaceConsistViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, SavedConsist?> ReplaceConsistCommand { get; }

    public required List<SavedConsist> SaveConsists { get; init; }

    public required Consist TargetConsist { get; init; }

    [ObservableProperty]
    private SavedConsist? _selectedConsist;

    public ReplaceConsistViewModel()
    {
        ReplaceConsistCommand = ReactiveCommand.Create(() => SelectedConsist);
    }
}
