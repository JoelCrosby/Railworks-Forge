using System.Reactive;

using CommunityToolkit.Mvvm.ComponentModel;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public partial class ConfirmationDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private string? _title;

    [ObservableProperty]
    private string? _bodyText;

    [ObservableProperty]
    private string? _acceptLabel;

    public ReactiveCommand<Unit, bool> AcceptClickedCommand { get; } = ReactiveCommand.Create(() => true);
    public ReactiveCommand<Unit, bool> CancelClickedCommand { get; } = ReactiveCommand.Create(() => false);
}
