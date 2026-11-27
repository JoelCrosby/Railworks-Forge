using System.ComponentModel;

using NSubstitute;

using RailworksForge.Core.Assets;
using RailworksForge.Core.Consists;
using RailworksForge.Core.Packaging;
using RailworksForge.Core.Scenarios;
using RailworksForge.Core.Serialization;
using RailworksForge.Core.Vehicles;
using RailworksForge.Services;
using RailworksForge.Tests.Support;
using RailworksForge.ViewModels;

namespace RailworksForge.Tests.ViewModels;

public class NavigationBarViewModelTests
{
    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();
    private readonly NavigationBarViewModel _navigationBar;

    public NavigationBarViewModelTests()
    {
        var tools = new ToolsMenuViewModel(
            new ToolsActivity(),
            Substitute.For<IStoragePickerService>(),
            Substitute.For<ISerzFileService>(),
            Substitute.For<IPackageInstallService>());

        _navigationBar = new NavigationBarViewModel(_navigation, tools, _dialogs);
    }

    [Fact]
    public void OnRoutesPage_OnlyRoutesCrumbIsCurrent()
    {
        Assert.True(_navigationBar.IsRoutesCrumbCurrent);
        Assert.True(_navigationBar.IsRouteCrumbCurrent);
        Assert.True(_navigationBar.IsScenarioCrumbCurrent);
        Assert.True(_navigationBar.IsConsistCrumbCurrent);
    }

    [Fact]
    public void OnScenarioPage_ScenarioCrumbIsCurrent()
    {
        _navigation.CurrentRoute.Returns(new RouteViewModel(TestData.Route()));
        _navigation.CurrentScenario.Returns(TestData.Scenario());

        Assert.False(_navigationBar.IsRoutesCrumbCurrent);
        Assert.False(_navigationBar.IsRouteCrumbCurrent);
        Assert.True(_navigationBar.IsScenarioCrumbCurrent);
    }

    [Fact]
    public void OpeningSettings_LeavesEveryCrumbClickable()
    {
        _navigation.IsSettingsActive.Returns(true);

        Assert.False(_navigationBar.IsRoutesCrumbCurrent);
        Assert.False(_navigationBar.IsRouteCrumbCurrent);
        Assert.False(_navigationBar.IsScenarioCrumbCurrent);
        Assert.False(_navigationBar.IsConsistCrumbCurrent);
    }

    [Fact]
    public void NavigationChange_RefreshesCrumbs()
    {
        var changed = new List<string?>();
        _navigationBar.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        RaiseNavigationChanged();

        Assert.Contains(nameof(NavigationBarViewModel.IsRoutesCrumbCurrent), changed);
        Assert.Contains(nameof(NavigationBarViewModel.IsRouteCrumbCurrent), changed);
        Assert.Contains(nameof(NavigationBarViewModel.IsScenarioCrumbCurrent), changed);
        Assert.Contains(nameof(NavigationBarViewModel.IsConsistCrumbCurrent), changed);
    }

    [Fact]
    public async Task ShowRoutes_WithoutPendingEdits_NavigatesWithoutAsking()
    {
        await _navigationBar.ShowRoutesCommand.ExecuteAsync(null);

        _navigation.Received(1).ShowRoutes();
        await _dialogs.DidNotReceive().Show(Arg.Any<ConfirmationDialogViewModel>());
    }

    [Fact]
    public async Task ShowRoutes_WithPendingEdits_DiscardsThemWhenConfirmed()
    {
        var editor = GivenPendingEdits();
        _dialogs.Show(Arg.Any<ConfirmationDialogViewModel>()).Returns(true);

        await _navigationBar.ShowRoutesCommand.ExecuteAsync(null);

        editor.Received(1).Discard();
        _navigation.Received(1).ShowRoutes();
    }

    [Fact]
    public async Task ShowRoute_WithPendingEdits_StaysWhenDeclined()
    {
        var editor = GivenPendingEdits();
        _dialogs.Show(Arg.Any<ConfirmationDialogViewModel>()).Returns(false);

        await _navigationBar.ShowRouteCommand.ExecuteAsync(null);

        editor.DidNotReceive().Discard();
        _navigation.DidNotReceive().ShowCurrentRoute();
    }

    [Fact]
    public async Task ShowScenario_FromConsistPage_KeepsPendingEdits()
    {
        var editor = GivenPendingEdits();
        var consistPage = CreateConsistPage(editor);
        _navigation.CurrentPage.Returns(consistPage);

        await _navigationBar.ShowScenarioCommand.ExecuteAsync(null);

        await _dialogs.DidNotReceive().Show(Arg.Any<ConfirmationDialogViewModel>());
        editor.DidNotReceive().Discard();
        _navigation.Received(1).ShowCurrentScenario();
    }

    [Fact]
    public async Task ShowScenario_OnScenarioPage_ReloadingAsksToDiscard()
    {
        var editor = GivenPendingEdits();
        var scenarioPage = CreateScenarioPage();
        _navigation.CurrentPage.Returns(scenarioPage);
        _dialogs.Show(Arg.Any<ConfirmationDialogViewModel>()).Returns(true);

        await _navigationBar.ShowScenarioCommand.ExecuteAsync(null);

        editor.Received(1).Discard();
        _navigation.Received(1).ShowCurrentScenario();
    }

    [Fact]
    public async Task ShowConsist_OnConsistPage_ReloadingAsksToDiscard()
    {
        var editor = GivenPendingEdits();
        var consistPage = CreateConsistPage(editor);
        _navigation.CurrentPage.Returns(consistPage);
        _dialogs.Show(Arg.Any<ConfirmationDialogViewModel>()).Returns(false);

        await _navigationBar.ShowConsistCommand.ExecuteAsync(null);

        editor.DidNotReceive().Discard();
        _navigation.DidNotReceive().ShowCurrentConsist();
    }

    [Fact]
    public void ShowSettings_OpensSettings()
    {
        _navigationBar.ShowSettingsCommand.Execute(null);

        _navigation.Received(1).ShowSettings();
    }

    [Fact]
    public void ShowSettings_WhenAlreadyOpen_ClosesSettings()
    {
        _navigation.IsSettingsActive.Returns(true);

        _navigationBar.ShowSettingsCommand.Execute(null);

        _navigation.Received(1).CloseSettings();
        _navigation.DidNotReceive().ShowSettings();
    }

    [Fact]
    public async Task GoBack_FromSettings_ReturnsToThePreviousPage()
    {
        _navigation.IsSettingsActive.Returns(true);

        await _navigationBar.GoBackCommand.ExecuteAsync(null);

        _navigation.Received(1).CloseSettings();
    }

    [Fact]
    public async Task GoBack_FromAConsist_ShowsItsScenarioWithoutDiscarding()
    {
        var editor = GivenPendingEdits();
        _navigation.CurrentConsist.Returns(TestData.Consist());

        await _navigationBar.GoBackCommand.ExecuteAsync(null);

        editor.DidNotReceive().Discard();
        _navigation.Received(1).ShowCurrentScenario();
    }

    [Fact]
    public async Task GoBack_FromARoute_ShowsTheRoutes()
    {
        _navigation.CurrentRoute.Returns(new RouteViewModel(TestData.Route()));
        RaiseNavigationChanged();

        await _navigationBar.GoBackCommand.ExecuteAsync(null);

        _navigation.Received(1).ShowRoutes();
    }

    [Fact]
    public void GoBack_OnTheRoutesPage_IsUnavailable()
    {
        RaiseNavigationChanged();

        Assert.False(_navigationBar.GoBackCommand.CanExecute(null));
    }

    [Fact]
    public void ScenarioCrumb_IsDirtyWhileTheEditorHasPendingChanges()
    {
        var editor = Substitute.For<IScenarioEditor>();
        _navigation.CurrentEditor.Returns(editor);
        RaiseNavigationChanged();

        Assert.False(_navigationBar.IsScenarioDirty);

        Fakes.SetPendingChanges(editor, true);

        Assert.True(_navigationBar.IsScenarioDirty);
    }

    private IScenarioEditor GivenPendingEdits()
    {
        var editor = Substitute.For<IScenarioEditor>();

        editor.HasPendingChanges.Returns(true);
        _navigation.CurrentEditor.Returns(editor);

        return editor;
    }

    private void RaiseNavigationChanged()
    {
        var changed = new PropertyChangedEventArgs(nameof(INavigationService.CurrentPage));

        _navigation.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(_navigation, changed);
    }

    private static ConsistDetailViewModel CreateConsistPage(IScenarioEditor editor)
    {
        return new ConsistDetailViewModel(
            editor,
            TestData.Consist(),
            Substitute.For<IVehicleIndexService>(),
            Substitute.For<ILauncherService>(),
            Substitute.For<IAssetDirectoryTreeService>(),
            Substitute.For<IRollingStockService>(),
            Substitute.For<IDialogService>(),
            Substitute.For<IImageService>());
    }

    private static ScenarioDetailViewModel CreateScenarioPage()
    {
        return new ScenarioDetailViewModel(
            TestData.Scenario(),
            Substitute.For<INavigationService>(),
            Substitute.For<IDialogService>(),
            Substitute.For<ILauncherService>(),
            Substitute.For<IImageService>(),
            Substitute.For<IScenarioService>(),
            Substitute.For<ISavedConsistService>());
    }
}
