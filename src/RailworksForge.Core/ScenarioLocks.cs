using System.Collections.Concurrent;

using RailworksForge.Core.Models;

namespace RailworksForge.Core;

// Serialises reads and writes of a scenario so a page loading it never parses a Scenario.bin mid-write.
public static class ScenarioLocks
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new(StringComparer.OrdinalIgnoreCase);

    public static async Task<IDisposable> Acquire(Scenario scenario, CancellationToken cancellationToken = default)
    {
        var semaphore = Locks.GetOrAdd(scenario.Id, _ => new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync(cancellationToken);

        return new Releaser(semaphore);
    }

    private sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
    {
        private int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) is 0)
            {
                semaphore.Release();
            }
        }
    }
}
