using System.ComponentModel;

using Avalonia.Threading;

using NSubstitute;

using RailworksForge.Core.Scenarios;

namespace RailworksForge.Tests.Support;

public static class Fakes
{
    public static IDispatcher ImmediateDispatcher()
    {
        var dispatcher = Substitute.For<IDispatcher>();

        dispatcher
            .When(fake => fake.Post(Arg.Any<Action>(), Arg.Any<DispatcherPriority>()))
            .Do(call => call.Arg<Action>().Invoke());

        return dispatcher;
    }

    public static IScenarioEditor Editor(IScenarioEditSession session, Scenario? scenario = null)
    {
        var editor = Substitute.For<IScenarioEditor>();

        editor.Scenario.Returns(scenario ?? TestData.Scenario());
        editor.GetSession(Arg.Any<CancellationToken>()).Returns(session);
        editor.PendingChanges.Returns([]);

        return editor;
    }

    public static void SetPendingChanges(IScenarioEditor editor, bool hasPendingChanges)
    {
        var changed = new PropertyChangedEventArgs(nameof(IScenarioEditor.HasPendingChanges));

        editor.HasPendingChanges.Returns(hasPendingChanges);
        editor.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(editor, changed);
    }
}
