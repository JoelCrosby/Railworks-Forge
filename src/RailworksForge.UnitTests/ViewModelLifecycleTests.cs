using RailworksForge.ViewModels;

namespace RailworksForge.UnitTests;

public class ViewModelLifecycleTests
{
    [Fact]
    public async Task Activate_MarksActiveAndRunsActivation()
    {
        var viewModel = new TestViewModel();

        await viewModel.Activate();

        Assert.True(viewModel.IsActive);
        Assert.Equal(1, viewModel.ActivationCount);
    }

    [Fact]
    public async Task Deactivate_CancelsLoadingAndMarksInactive()
    {
        var viewModel = new TestViewModel();
        var pending = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        await viewModel.Activate();
        var load = viewModel.Loading.RunAsync("Loading", token =>
        {
            entered.SetResult(token);

            return pending.Task;
        }, _ => { });
        var token = await entered.Task;

        viewModel.Deactivate();
        pending.SetResult(1);
        await load;

        Assert.False(viewModel.IsActive);
        Assert.True(token.IsCancellationRequested);
        Assert.False(viewModel.IsLoading);
        Assert.True(viewModel.WasDeactivated);
    }

    private sealed class TestViewModel : ViewModelBase
    {
        public int ActivationCount { get; private set; }

        public bool WasDeactivated { get; private set; }

        protected override Task OnActivated()
        {
            ActivationCount++;

            return Task.CompletedTask;
        }

        protected override void OnDeactivated()
        {
            WasDeactivated = true;
        }
    }
}
