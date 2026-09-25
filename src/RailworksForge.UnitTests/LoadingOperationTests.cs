using System.Collections.Concurrent;
using System.Reactive.Linq;

using RailworksForge.ViewModels;

namespace RailworksForge.UnitTests;

public class LoadingOperationTests
{
    [Fact]
    public async Task Failure_StopsLoadingAndCanBeRetried()
    {
        var operation = new LoadingOperation();
        var attempts = 0;
        var applied = false;
        await operation.RunAsync("Loading test data", _ =>
        {

            if (++attempts == 1)
            {
                throw new IOException("Unavailable");
            }

            return Task.FromResult(true);
        }, result => applied = result);

        Assert.False(operation.IsLoading);
        Assert.Equal("Unavailable", operation.ErrorMessage);
        Assert.True(operation.CanRetry);

        await operation.RetryCommand.Execute();

        Assert.True(applied);
        Assert.False(operation.IsLoading);
        Assert.False(operation.HasError);
        Assert.False(operation.IsVisible);
    }

    [Fact]
    public async Task SupersededLoad_CannotPublishOrStopCurrentSpinner()
    {
        var operation = new LoadingOperation();
        var first = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var results = new List<int>();
        var firstLoad = operation.RunAsync("First", token =>
        {
            entered.SetResult(token);

            return first.Task;
        }, results.Add);
        var firstToken = await entered.Task;
        var secondLoad = operation.RunAsync("Second", _ => second.Task, results.Add);

        Assert.True(firstToken.IsCancellationRequested);
        first.SetResult(1);
        await firstLoad;

        Assert.Empty(results);
        Assert.True(operation.IsLoading);
        Assert.Equal("Second", operation.Message);

        second.SetResult(2);
        await secondLoad;

        Assert.Equal([2], results);
        Assert.False(operation.IsLoading);
    }

    [Fact]
    public async Task Cancel_IgnoresLateResultAndDoesNotShowAnError()
    {
        var operation = new LoadingOperation();
        var result = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var applied = false;
        var load = operation.RunAsync("Loading", _ =>
        {
            entered.SetResult();

            return result.Task;
        }, _ => applied = true);
        await entered.Task;
        operation.Cancel();
        result.SetResult(1);
        await load;

        Assert.False(applied);
        Assert.False(operation.IsLoading);
        Assert.False(operation.HasError);
    }

    [Fact]
    public async Task OlderFailure_DoesNotReplaceNewResultsWithAnError()
    {
        var operation = new LoadingOperation();
        var first = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var oldLoad = operation.RunAsync("Old", _ =>
        {
            entered.SetResult();

            return first.Task;
        }, _ => { });
        await entered.Task;
        await operation.RunAsync("New", _ => Task.FromResult(2), _ => { });
        first.SetException(new IOException("Old failure"));
        await oldLoad;

        Assert.False(operation.HasError);
        Assert.False(operation.IsLoading);
    }

    [Fact]
    public async Task MutationFailure_DoesNotOfferAutomaticRetry()
    {
        var operation = new LoadingOperation();
        await operation.RunAsync("Saving", _ => Task.FromException(new IOException("Write failed")));

        Assert.True(operation.HasError);
        Assert.False(operation.CanRetry);
        Assert.False(operation.IsLoading);
    }

    [Fact]
    public async Task EmptyResult_StillCompletesLoading()
    {
        var operation = new LoadingOperation();
        var applied = false;
        await operation.RunAsync("Loading", _ => Task.FromResult(Array.Empty<int>()), _ => applied = true);

        Assert.True(applied);
        Assert.False(operation.IsLoading);
        Assert.False(operation.HasError);
    }

    [Fact]
    public void WorkRunsOffUiThread_AndResultsAndNotificationsReturnToUiThread()
    {
        var previous = SynchronizationContext.Current;
        using var context = new PumpContext();
        SynchronizationContext.SetSynchronizationContext(context);

        try
        {
            var uiThread = Environment.CurrentManagedThreadId;
            var workerThread = uiThread;
            var appliedThread = 0;
            var operation = new LoadingOperation();
            operation.PropertyChanged += (_, _) => Assert.Equal(uiThread, Environment.CurrentManagedThreadId);
            var load = operation.RunAsync("Loading", _ =>
            {
                workerThread = Environment.CurrentManagedThreadId;

                return Task.FromResult(1);
            }, _ => appliedThread = Environment.CurrentManagedThreadId);

            Assert.True(operation.IsLoading);

            while (!load.IsCompleted)
            {
                context.RunNext();
            }

            Assert.True(load.IsCompletedSuccessfully, load.Exception?.ToString());
            Assert.NotEqual(uiThread, workerThread);
            Assert.Equal(uiThread, appliedThread);
            Assert.False(operation.IsLoading);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    private sealed class PumpContext : SynchronizationContext, IDisposable
    {
        private readonly BlockingCollection<Action> _callbacks = new();

        public override void Post(SendOrPostCallback callback, object? state)
        {
            _callbacks.Add(() => callback(state));
        }

        public void RunNext()
        {
            Assert.True(_callbacks.TryTake(out var callback, TimeSpan.FromSeconds(10)));
            callback();
        }

        public void Dispose()
        {
            _callbacks.Dispose();
        }
    }
}
