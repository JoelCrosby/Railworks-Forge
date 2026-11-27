using Avalonia.Headless;

namespace RailworksForge.Tests.Components;

// Avalonia allows one headless application per process, and its controls belong to that session's thread, so every
// test that creates controls runs through this session.
public static class ComponentSession
{
    public const string CollectionName = "Avalonia UI";

    private static readonly Lazy<HeadlessUnitTestSession> Session = new(() => HeadlessUnitTestSession.StartNew(typeof(ComponentTestApp)));

    public static Task Run(Action test)
    {
        return Session.Value.Dispatch(test, CancellationToken.None);
    }
}
