using NSubstitute;

using RailworksForge.Core.Assets;
using RailworksForge.Core.Consists;
using RailworksForge.Core.Scenarios;
using RailworksForge.Services;
using RailworksForge.Tests.Support;
using RailworksForge.Translations;
using RailworksForge.ViewModels;

namespace RailworksForge.Tests.ViewModels;

[Collection(GlobalStateCollection.Name)]
public class ScenarioDetailViewModelTests
{
    private readonly Scenario _scenario = TestData.Scenario();
    private readonly IScenarioEditSession _session = Substitute.For<IScenarioEditSession>();
    private readonly IScenarioEditor _editor;
    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();
    private readonly ILauncherService _launcher = Substitute.For<ILauncherService>();
    private readonly IScenarioService _scenarios = Substitute.For<IScenarioService>();
    private readonly ISavedConsistService _savedConsists = Substitute.For<ISavedConsistService>();
    private readonly Consist _express = TestData.Consist("1A01");
    private readonly Consist _freight = TestData.Consist("4M11");
    private readonly ScenarioDetailViewModel _page;

    public ScenarioDetailViewModelTests()
    {
        _editor = Fakes.Editor(_session, _scenario);
        _scenarios.CreateEditor(_scenario).Returns(_editor);
        _session.GetConsists(Arg.Any<CancellationToken>()).Returns([_express, _freight]);

        _page = new ScenarioDetailViewModel(
            _scenario,
            _navigation,
            _dialogs,
            _launcher,
            Substitute.For<IImageService>(),
            _scenarios,
            _savedConsists);
    }

    [Fact]
    public void Editor_IsCreatedForThisScenario()
    {
        Assert.Same(_editor, _page.Editor);
    }

    [Fact]
    public async Task Activate_ListsTheScenarioServices()
    {
        await _page.Activate();

        Assert.Equal([_express, _freight], _page.Services.Select(service => service.Consist));
        _editor.Received(1).Update();
    }

    [Fact]
    public async Task SearchTerm_FiltersServices()
    {
        await _page.Activate();

        _page.SearchTerm = "4m11";

        Assert.Same(_freight, Assert.Single(_page.Services).Consist);
    }

    [Fact]
    public async Task SingleServiceCommands_NeedExactlyOneSelectedService()
    {
        await _page.Activate();

        Assert.False(_page.OpenServiceCommand.CanExecute(null));
        Assert.False(_page.SaveConsistCommand.CanExecute(null));

        SelectServices(0);

        Assert.True(_page.OpenServiceCommand.CanExecute(null));
        Assert.True(_page.SaveConsistCommand.CanExecute(null));

        SelectServices(0, 1);

        Assert.False(_page.OpenServiceCommand.CanExecute(null));
        Assert.False(_page.SaveConsistCommand.CanExecute(null));
    }

    [Fact]
    public async Task BulkCommands_NeedAtLeastOneSelectedService()
    {
        await _page.Activate();

        Assert.False(_page.ReplaceConsistCommand.CanExecute(null));
        Assert.False(_page.DeleteConsistCommand.CanExecute(null));

        SelectServices(0, 1);

        Assert.True(_page.ReplaceConsistCommand.CanExecute(null));
        Assert.True(_page.DeleteConsistCommand.CanExecute(null));
    }

    [Fact]
    public async Task OpenService_ShowsTheConsistWithTheSharedEditor()
    {
        await _page.Activate();
        SelectServices(1);

        _page.OpenServiceCommand.Execute(null);

        _navigation.Received(1).ShowConsist(_editor, _freight);
    }

    [Fact]
    public async Task DeleteConsist_Confirmed_DeletesEverySelectedConsist()
    {
        await _page.Activate();
        SelectServices(0, 1);
        _dialogs.Show(Arg.Any<ConfirmationDialogViewModel>()).Returns(true);

        await _page.DeleteConsistCommand.ExecuteAsync(null);

        await _session.Received(1).DeleteConsists(Arg.Is<IEnumerable<Consist>>(targets => targets.SequenceEqual(new[] { _express, _freight })));
    }

    [Fact]
    public async Task DeleteConsist_Declined_DeletesNothing()
    {
        await _page.Activate();
        SelectServices(0);
        _dialogs.Show(Arg.Any<ConfirmationDialogViewModel>()).Returns(false);

        await _page.DeleteConsistCommand.ExecuteAsync(null);

        await _session.DidNotReceiveWithAnyArgs().DeleteConsists(default!);
    }

    [Fact]
    public async Task DeleteConsist_AsksAboutEachConsistOrTheWholeSelection()
    {
        var questions = new List<ConfirmationDialogViewModel>();
        _dialogs.Show(Arg.Do<ConfirmationDialogViewModel>(questions.Add)).Returns(false);
        await _page.Activate();

        SelectServices(0);
        await _page.DeleteConsistCommand.ExecuteAsync(null);
        SelectServices(0, 1);
        await _page.DeleteConsistCommand.ExecuteAsync(null);

        Assert.Equal(Strings.delete_consist.CurrentValue, questions[0].AcceptLabel);
        Assert.Equal(Strings.delete_consists.CurrentValue, questions[1].AcceptLabel);
    }

    [Fact]
    public async Task ReplaceConsist_ReplacesTheSelectionWithTheChosenConsist()
    {
        var replacement = TestData.PreloadConsist();
        var dialog = CreateReplaceConsistDialog();
        _dialogs.Create<ReplaceConsistViewModel>().Returns(dialog);
        _dialogs.Show(dialog).Returns(replacement);
        await _page.Activate();
        SelectServices(1);

        await _page.ReplaceConsistCommand.ExecuteAsync(null);

        await _session.Received(1).ReplaceConsists(Arg.Is<IEnumerable<Consist>>(targets => targets.Single() == _freight), replacement);
    }

    [Fact]
    public async Task ReplaceConsist_CancelledDialog_ReplacesNothing()
    {
        var dialog = CreateReplaceConsistDialog();
        _dialogs.Create<ReplaceConsistViewModel>().Returns(dialog);
        _dialogs.Show(dialog).Returns((PreloadConsist?)null);
        await _page.Activate();
        SelectServices(1);

        await _page.ReplaceConsistCommand.ExecuteAsync(null);

        await _session.DidNotReceiveWithAnyArgs().ReplaceConsists(default!, default!);
    }

    [Fact]
    public async Task EditDetails_BuffersTheEditedDetailsInTheSession()
    {
        var details = TestData.ScenarioDetails();
        var edited = details with { DurationMinutes = 95 };
        _session.GetDetails().Returns(details);
        _dialogs.Show(Arg.Any<EditScenarioDetailsViewModel>()).Returns(edited);
        await _page.Activate();

        await _page.EditDetailsCommand.ExecuteAsync(null);

        await _session.Received(1).UpdateDetails(edited);
        _navigation.Received().UpdateCurrentScenario(_scenario);
    }

    [Fact]
    public async Task EditDetails_ShowsTheRenamedScenarioInTheHeader()
    {
        var details = TestData.ScenarioDetails();
        var renamed = _scenario with { Name = "1A01 Euston - Glasgow (Sundays)" };
        _session.GetDetails().Returns(details);
        _dialogs.Show(Arg.Any<EditScenarioDetailsViewModel>()).Returns(details with { DurationMinutes = 95 });
        await _page.Activate();
        _editor.Scenario.Returns(renamed);

        await _page.EditDetailsCommand.ExecuteAsync(null);

        Assert.Same(renamed, _page.Scenario);
        _navigation.Received().UpdateCurrentScenario(renamed);
    }

    [Fact]
    public async Task EditDetails_CancelledDialog_ChangesNothing()
    {
        _session.GetDetails().Returns(TestData.ScenarioDetails());
        _dialogs.Show(Arg.Any<EditScenarioDetailsViewModel>()).Returns((ScenarioDetails?)null);
        await _page.Activate();

        await _page.EditDetailsCommand.ExecuteAsync(null);

        await _session.DidNotReceiveWithAnyArgs().UpdateDetails(default!);
    }

    [Fact]
    public async Task PendingChanges_DescribeAnEditedDetailsChange()
    {
        await _page.Activate();
        var change = new ScenarioChange(ScenarioChangeKind.EditDetails, string.Empty, 1);
        _editor.PendingChanges.Returns([change]);

        Fakes.SetPendingChanges(_editor, true);

        Assert.Contains(Strings.change_edited_details.CurrentValue, _page.PendingChanges.Details);
    }

    [Fact]
    public async Task SaveConsist_SavesUnderTheChosenName()
    {
        _dialogs.Show(Arg.Any<SaveConsistViewModel>()).Returns("My Pendolino");
        await _page.Activate();
        SelectServices(0);

        await _page.SaveConsistCommand.ExecuteAsync(null);

        await _savedConsists.Received(1).Save(_editor, _express, "My Pendolino", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveConsist_SuggestsTheLocomotiveName()
    {
        SaveConsistViewModel? shown = null;
        _dialogs.Show(Arg.Do<SaveConsistViewModel>(dialog => shown = dialog)).Returns((string?)null);
        await _page.Activate();
        SelectServices(0);

        await _page.SaveConsistCommand.ExecuteAsync(null);

        Assert.Equal(_express.LocomotiveName, shown?.Name);
        await _savedConsists.DidNotReceiveWithAnyArgs().Save(default!, default!, default!, default);
    }

    [Fact]
    public void EditCommands_FollowTheEditorsPendingChanges()
    {
        Assert.False(_page.ApplyChangesCommand.CanExecute(null));
        Assert.False(_page.DiscardChangesCommand.CanExecute(null));

        Fakes.SetPendingChanges(_editor, true);

        Assert.True(_page.ApplyChangesCommand.CanExecute(null));
        Assert.True(_page.DiscardChangesCommand.CanExecute(null));
    }

    [Fact]
    public async Task ApplyChanges_WhenTheScenarioChangedOnDisk_ShowsWhyNothingWasSaved()
    {
        Fakes.SetPendingChanges(_editor, true);
        _editor.Apply(Arg.Any<CancellationToken>()).Returns(false);

        await _page.ApplyChangesCommand.ExecuteAsync(null);

        Assert.Equal(Strings.scenario_changed_on_disk.CurrentValue, _page.Loading.ErrorMessage);
        Assert.False(_page.Loading.CanRetry);
    }

    [Fact]
    public async Task ApplyChanges_Saved_ShowsNoError()
    {
        Fakes.SetPendingChanges(_editor, true);
        _editor.Apply(Arg.Any<CancellationToken>()).Returns(true);

        await _page.ApplyChangesCommand.ExecuteAsync(null);

        Assert.False(_page.Loading.HasError);
        _editor.Received(1).Update();
    }

    [Fact]
    public async Task DiscardChanges_DiscardsAndReloadsTheServices()
    {
        Fakes.SetPendingChanges(_editor, true);

        await _page.DiscardChangesCommand.ExecuteAsync(null);

        _editor.Received(1).Discard();
        Assert.Equal(2, _page.Services.Count);
    }

    [Fact]
    public async Task ExportBinToXml_OpensTheExportedFilesFolder()
    {
        var exportDirectory = Path.Join(Path.GetTempPath(), "exports", "scenario");
        _scenarios.ExportBinToXml(_scenario).Returns(Path.Join(exportDirectory, "Scenario.bin.xml"));

        await _page.ExportBinToXmlCommand.ExecuteAsync(null);

        _launcher.Received(1).OpenDirectory(exportDirectory);
    }

    [Fact]
    public async Task ScenarioFileCommands_ActOnThisScenario()
    {
        await _page.ConvertXmlToBinCommand.ExecuteAsync(null);
        await _page.ExtractScenariosCommand.ExecuteAsync(null);
        _page.OpenInExplorerCommand.Execute(null);
        _page.OpenBackupsFolderCommand.Execute(null);

        await _scenarios.Received(1).ConvertXmlToBin(_scenario);
        _scenarios.Received(1).ExtractScenarios(_scenario.Route);
        _launcher.Received(1).OpenDirectory(_scenario.BrowsableDirectoryPath);
        _launcher.Received(1).OpenOrCreateDirectory(_scenario.BackupDirectory);
    }

    private void SelectServices(params int[] indexes)
    {
        _page.SelectedServices.Clear();

        foreach (var index in indexes)
        {
            _page.SelectedServices.Add(_page.Services[index]);
        }
    }

    private static ReplaceConsistViewModel CreateReplaceConsistDialog()
    {
        return new ReplaceConsistViewModel(
            Substitute.For<IAssetDirectoryTreeService>(),
            Substitute.For<IPreloadConsistService>(),
            Substitute.For<IImageService>(),
            Substitute.For<ILauncherService>());
    }

    [Fact]
    public async Task Selection_IsCountedAndCanBeCleared()
    {
        await _page.Activate();

        _page.SelectedServices.Add(_page.Services[0]);
        _page.SelectedServices.Add(_page.Services[1]);

        Assert.Equal(2, _page.SelectedCount);
        Assert.Equal(string.Format(Strings.items_selected.CurrentValue, 2), _page.SelectionText);

        _page.ClearSelectionCommand.Execute(null);

        Assert.Equal(0, _page.SelectedCount);
    }

    [Fact]
    public async Task PendingChanges_DescribeTheEditorsChanges()
    {
        await _page.Activate();
        var change = new ScenarioChange(ScenarioChangeKind.DeleteConsists, "4M11", 1);
        _editor.PendingChanges.Returns([change]);

        Fakes.SetPendingChanges(_editor, true);

        Assert.True(_page.PendingChanges.HasChanges);
        Assert.Equal(Strings.unsaved_change_one.CurrentValue, _page.PendingChanges.Summary);
        Assert.Contains(string.Format(Strings.change_deleted_consist.CurrentValue, "4M11"), _page.PendingChanges.Details);
    }
}
