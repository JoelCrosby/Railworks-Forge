using NSubstitute;

using RailworksForge.Core.Assets;
using RailworksForge.Core.Routes;
using RailworksForge.Core.Vehicles;
using RailworksForge.Tests.Support;
using RailworksForge.Translations;
using RailworksForge.ViewModels;

namespace RailworksForge.Tests.ViewModels;

[Collection(GlobalStateCollection.Name)]
public class ProgressDialogTests
{
    [Fact]
    public async Task CheckAssets_ListsTheMissingBlueprints()
    {
        var route = TestData.Route();
        var missing = TestData.Blueprint(@"Scenery\Missing.xml");
        var assetCheck = Substitute.For<IRouteAssetCheckService>();
        assetCheck.FindMissingAssets(route, Arg.Any<IProgress<AssetCheckProgress>>(), Arg.Any<CancellationToken>()).Returns([missing]);
        var dialog = new CheckAssetsViewModel(route, assetCheck);

        await dialog.Activate();

        Assert.Equal([missing], dialog.Blueprints);
        Assert.False(dialog.IsLoading);
    }

    [Fact]
    public async Task CheckAssets_ShowsProgressForEachStage()
    {
        var assetCheck = Substitute.For<IRouteAssetCheckService>();
        assetCheck
            .FindMissingAssets(Arg.Any<Route>(), Arg.Any<IProgress<AssetCheckProgress>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var progress = call.Arg<IProgress<AssetCheckProgress>>();

                progress.Report(new AssetCheckProgress { Stage = AssetCheckStage.ReadingFiles, Percentage = 50, Completed = 5, Total = 10 });

                return new List<Blueprint>();
            });
        var dialog = new CheckAssetsViewModel(TestData.Route(), assetCheck);

        await dialog.Activate();

        var expectedMessage = string.Format(Strings.processed_files.CurrentValue, 5, 10);

        await Wait.Until(() => dialog.LoadingMessage == expectedMessage, "progress to be reported");
        Assert.Equal(50, dialog.LoadingProgress);
    }

    [Fact]
    public async Task VehicleIndex_CompletedRefresh_ShowsTheFinalSummary()
    {
        var index = Substitute.For<IVehicleIndex>();
        var completed = new VehicleIndexProgress
        {
            Sources = 10,
            TotalSources = 10,
            Vehicles = 120,
            Skipped = 2,
            Stage = VehicleIndexStage.Completed,
        };
        index.RefreshAsync(Arg.Any<IProgress<VehicleIndexProgress>?>(), Arg.Any<CancellationToken>()).Returns(completed);
        var dialog = new VehicleIndexDialogViewModel(index);

        await dialog.Activate();

        Assert.False(dialog.IsDiscovering);
        Assert.Equal(100, dialog.ProgressPercent);
        Assert.Equal(Strings.vehicle_index_complete.CurrentValue, dialog.Status);
        Assert.Equal(string.Format(Strings.vehicle_index_progress.CurrentValue, 10, 120, 2), dialog.Summary);
    }

    [Fact]
    public async Task VehicleIndex_ProgressReportsWhileIndexing_UpdateThePercentage()
    {
        var index = Substitute.For<IVehicleIndex>();
        var release = new TaskCompletionSource<VehicleIndexProgress>(TaskCreationOptions.RunContinuationsAsynchronously);
        index
            .RefreshAsync(Arg.Any<IProgress<VehicleIndexProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var progress = call.Arg<IProgress<VehicleIndexProgress>?>()!;

                progress.Report(new VehicleIndexProgress { Sources = 1, TotalSources = 4, Stage = VehicleIndexStage.Scanning });

                return release.Task;
            });
        var dialog = new VehicleIndexDialogViewModel(index);

        var activation = dialog.Activate();

        var expectedFileProgress = string.Format(Strings.vehicle_index_files.CurrentValue, 1, 4);

        await Wait.Until(() => dialog.Summary is not null, "progress to be reported");
        Assert.Equal(25, dialog.ProgressPercent);
        Assert.False(dialog.IsDiscovering);
        Assert.Equal(expectedFileProgress, dialog.FileProgress);

        release.SetResult(new VehicleIndexProgress { Sources = 4, TotalSources = 4, Stage = VehicleIndexStage.Completed });
        await activation;
    }

    [Fact]
    public async Task VehicleIndex_CancelIndexing_StopsTheRefresh()
    {
        var index = Substitute.For<IVehicleIndex>();
        var entered = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<VehicleIndexProgress>(TaskCreationOptions.RunContinuationsAsynchronously);
        index
            .RefreshAsync(Arg.Any<IProgress<VehicleIndexProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                entered.SetResult(call.Arg<CancellationToken>());

                return release.Task;
            });
        var dialog = new VehicleIndexDialogViewModel(index);
        var activation = dialog.Activate();
        var token = await entered.Task;

        dialog.CancelIndexingCommand.Execute(null);
        release.SetCanceled(token);
        await activation;

        Assert.True(token.IsCancellationRequested);
        Assert.False(dialog.IsLoading);
        Assert.False(dialog.IsDiscovering);
        Assert.Equal(Strings.vehicle_index_cancelled.CurrentValue, dialog.Status);
    }

    [Fact]
    public async Task VehicleIndex_FailedRefresh_ShowsTheErrorWithoutRetry()
    {
        var index = Substitute.For<IVehicleIndex>();
        index
            .RefreshAsync(Arg.Any<IProgress<VehicleIndexProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<VehicleIndexProgress>(new IOException("Database locked")));
        var dialog = new VehicleIndexDialogViewModel(index);

        await dialog.Activate();

        Assert.Equal("Database locked", dialog.Loading.ErrorMessage);
        Assert.False(dialog.Loading.CanRetry);
        Assert.False(dialog.IsDiscovering);
    }
}
