namespace RailworksForge.Tests.Support;

// The translation culture and LoadingOperation.UnobservedFailure are process-wide, so tests that change or observe them
// run one at a time.
[CollectionDefinition(Name, DisableParallelization = true)]
public class GlobalStateCollection
{
    public const string Name = "Global state";
}
