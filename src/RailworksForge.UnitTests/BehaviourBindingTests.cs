using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Xaml.Interactivity;

using CommunityToolkit.Mvvm.Input;

using RailworksForge.Behaviours;

namespace RailworksForge.UnitTests;

// The page behaviours bind to the view model, which only works if they inherit the control's DataContext.
public class BehaviourBindingTests
{
    [Fact]
    public void Behaviour_BindsAgainstTheAssociatedControlsDataContext()
    {
        var command = new RelayCommand(() => { });
        var behaviour = new ItemDoubleTapCommandBehaviour();
        var control = new Border();

        behaviour.Bind(ItemDoubleTapCommandBehaviour.CommandProperty, new Binding(nameof(CommandHost.Command)));
        Interaction.GetBehaviors(control).Add(behaviour);
        control.DataContext = new CommandHost(command);

        Assert.Same(command, behaviour.Command);
    }

    private sealed record CommandHost(RelayCommand Command);
}
