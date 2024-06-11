using System;
using System.Reactive;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public class MainMenuViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, Unit> ExitCommand { get; } = ReactiveCommand.Create(() => Environment.Exit(0));
}
