using System.ComponentModel;
using System.Globalization;

using Echoes;

using NSubstitute;

using RailworksForge.Core.Config;
using RailworksForge.Core.Packaging;
using RailworksForge.Services;
using RailworksForge.Tests.Support;
using RailworksForge.Translations;
using RailworksForge.ViewModels;

namespace RailworksForge.Tests.ViewModels;

[Collection(GlobalStateCollection.Name)]
public class ShellIndicatorTests
{
    [Fact]
    public void UpdateProgress_ShowsStageAndFileProgress()
    {
        var indicator = new ProgressIndicatorViewModel();

        indicator.UpdateProgress(new InstallProgress
        {
            Stage = InstallStage.Installing,
            PackageName = "WCML.rwp",
            Progress = 40,
            FilesProcessed = 4,
            FileCount = 10,
            IsLoading = true,
        });

        Assert.True(indicator.IsVisible);
        Assert.True(indicator.IsLoading);
        Assert.Equal(40, indicator.Progress);
        Assert.Equal(string.Format(Strings.installing_package.CurrentValue, "WCML.rwp"), indicator.StatusMessage);
        Assert.Equal(string.Format(Strings.processing_file.CurrentValue, 4, 10), indicator.ProgressMessage);
    }

    [Fact]
    public void UpdateProgress_WithoutFiles_LeavesFileProgressEmpty()
    {
        var indicator = new ProgressIndicatorViewModel();

        indicator.UpdateProgress(new InstallProgress
        {
            Stage = InstallStage.AlreadyInstalled,
            PackageName = "WCML.rwp",
            Progress = 100,
            IsLoading = false,
        });

        Assert.Equal(string.Empty, indicator.ProgressMessage);
        Assert.Equal(string.Format(Strings.package_already_installed.CurrentValue, "WCML.rwp"), indicator.StatusMessage);
    }

    [Fact]
    public void ClearProgress_HidesAndResetsTheIndicator()
    {
        var indicator = new ProgressIndicatorViewModel
        {
            IsVisible = true,
            IsLoading = true,
            Progress = 50,
            StatusMessage = "Installing",
            ProgressMessage = "1 of 2",
        };

        indicator.ClearProgress();

        Assert.False(indicator.IsVisible);
        Assert.False(indicator.IsLoading);
        Assert.Equal(0, indicator.Progress);
        Assert.Empty(indicator.StatusMessage);
        Assert.Empty(indicator.ProgressMessage);
    }

    [Fact]
    public void StatusBar_ShowsTheCurrentPagesStatus()
    {
        var navigation = Substitute.For<INavigationService>();
        var statusBar = new StatusBarViewModel(navigation);
        var page = new StatusPage("12 services");

        navigation.CurrentPage.Returns(page);
        navigation.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(navigation, new PropertyChangedEventArgs(nameof(INavigationService.CurrentPage)));

        Assert.Equal("12 services", statusBar.StatusText);
    }

    [Fact]
    public void StatusBar_FollowsTheStatusOfThePageItShows()
    {
        var navigation = Substitute.For<INavigationService>();
        var statusBar = new StatusBarViewModel(navigation);
        var page = new StatusPage("12 services");
        var changed = new List<string?>();

        navigation.CurrentPage.Returns(page);
        navigation.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(navigation, new PropertyChangedEventArgs(nameof(INavigationService.CurrentPage)));
        statusBar.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        page.Update("12 services · 2 selected");

        Assert.Contains(nameof(StatusBarViewModel.StatusText), changed);
        Assert.Equal("12 services · 2 selected", statusBar.StatusText);
    }

    private sealed class StatusPage(string status) : ViewModelBase
    {
        private string _status = status;

        public override string StatusText => _status;

        public void Update(string status)
        {
            _status = status;
            OnPropertyChanged(nameof(StatusText));
        }
    }
}
