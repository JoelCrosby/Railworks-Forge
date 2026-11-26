namespace RailworksForge.Tests.Support;

// Some loads are started fire-and-forget from property setters, so tests wait for their visible outcome.
public static class Wait
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public static async Task Until(Func<bool> condition, string description = "condition")
    {
        var deadline = DateTime.UtcNow + Timeout;

        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException($"Timed out waiting for {description}");
            }

            await Task.Delay(10);
        }
    }
}
