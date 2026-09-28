using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using RailworksForge.Core;
using RailworksForge.Core.Models;
using RailworksForge.Services;
using RailworksForge.Util;
using RailworksForge.Translations;

namespace RailworksForge.ViewModels;

public partial class ScenarioDetailViewModel : ViewModelBase
{
    private readonly NavigationService _navigation;
    private readonly DialogService _dialogs;
    private readonly LauncherService _launcher;
    private readonly ImageService _images;
    private readonly BackgroundImageLoader _imageLoader = new();

    [ObservableProperty]
    public partial Scenario Scenario { get; set; }

    [ObservableProperty]
    public partial string? SearchTerm { get; set; }

    public SearchableCollection<ConsistViewModel> Services { get; } = new(service => service.Consist.SearchIndex);

    public ObservableCollection<ConsistViewModel> SelectedServices { get; } = [];

    public ScenarioEditor Editor { get; }

    public ScenarioDetailViewModel(
        Scenario scenario,
        NavigationService navigation,
        DialogService dialogs,
        LauncherService launcher,
        ImageService images)
    {
        Scenario = scenario;
        _navigation = navigation;
        _dialogs = dialogs;
        _launcher = launcher;
        _images = images;

        Editor = new ScenarioEditor(scenario);

        SelectedServices.CollectionChanged += (_, _) => NotifySelectionCommands();
        Editor.PropertyChanged += (_, _) => NotifyEditCommands();
    }

    private ConsistViewModel? SingleSelectedService => SelectedServices.Count is 1 ? SelectedServices[0] : null;

    private bool HasSingleSelectedService => SingleSelectedService is not null;

    private bool HasSelectedServices => SelectedServices.Count > 0;

    protected override Task OnActivated()
    {
        return LoadServices();
    }

    protected override void OnDeactivated()
    {
        _imageLoader.Cancel();
    }

    partial void OnSearchTermChanged(string? value)
    {
        Services.Filter(value);
    }

    private Task LoadServices()
    {
        return Loading.RunAsync(Strings.loading_scenario_services.CurrentValue, async token =>
        {
            var session = await Editor.GetSession(token);

            return GetServices(session, token);
        }, ShowServices);
    }

    // Edits are buffered in the editor's session and only written to the scenario when applied.
    private Task Edit(Func<ScenarioEditSession, Task> edit)
    {
        return Loading.RunAsync(Strings.updating_scenario.CurrentValue, async token =>
        {
            var session = await Editor.GetSession(token);
            await edit(session);

            return GetServices(session, token);
        }, ShowServices, allowRetry: false);
    }

    private static List<ConsistViewModel> GetServices(ScenarioEditSession session, CancellationToken token)
    {
        return session.GetConsists(token).Select(consist => new ConsistViewModel(consist)).ToList();
    }

    private void ShowServices(List<ConsistViewModel> services)
    {
        Editor.Update();
        Scenario = Editor.Scenario;
        Services.Reset(services);
        _imageLoader.Load(
            services,
            service => _images.GetConsistImage(service.Consist),
            (service, image) => service.ImageBitmap = image);
    }

    private bool HasPendingChanges => Editor.HasPendingChanges;

    [RelayCommand(CanExecute = nameof(HasPendingChanges))]
    private Task ApplyChanges()
    {
        return Loading.RunAsync(Strings.applying_changes.CurrentValue, Editor.Apply, applied =>
        {
            Editor.Update();
            Scenario = Editor.Scenario;

            if (!applied)
            {
                Loading.ShowError(Strings.scenario_changed_on_disk.CurrentValue);
            }
        }, allowRetry: false);
    }

    [RelayCommand(CanExecute = nameof(HasPendingChanges))]
    private Task DiscardChanges()
    {
        Editor.Discard();

        return LoadServices();
    }

    private void NotifyEditCommands()
    {
        ApplyChangesCommand.NotifyCanExecuteChanged();
        DiscardChangesCommand.NotifyCanExecuteChanged();
    }

    private void NotifySelectionCommands()
    {
        OpenServiceCommand.NotifyCanExecuteChanged();
        SaveConsistCommand.NotifyCanExecuteChanged();
        ReplaceConsistCommand.NotifyCanExecuteChanged();
        DeleteConsistCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void OpenInExplorer()
    {
        _launcher.OpenDirectory(Scenario.BrowsableDirectoryPath);
    }

    [RelayCommand]
    private void OpenBackupsFolder()
    {
        _launcher.OpenOrCreateDirectory(Scenario.BackupDirectory);
    }

    [RelayCommand]
    private Task ExportBinToXml()
    {
        return Loading.RunAsync(Strings.exporting_scenario_xml.CurrentValue, _ => Scenario.ExportBinToXml(), path =>
        {
            if (Path.GetDirectoryName(path) is {} directory)
            {
                _launcher.OpenDirectory(directory);
            }
        }, allowRetry: false);
    }

    [RelayCommand]
    private Task ConvertXmlToBin()
    {
        return Loading.RunAsync(Strings.converting_scenario_xml.CurrentValue, _ => Scenario.ConvertXmlToBin());
    }

    [RelayCommand]
    private Task ExtractScenarios()
    {
        return Loading.RunAsync(Strings.extracting_scenarios.CurrentValue, _ =>
        {
            Scenario.Route.ExtractScenarios();

            return Task.CompletedTask;
        });
    }

    [RelayCommand(CanExecute = nameof(HasSingleSelectedService))]
    private void OpenService()
    {
        _navigation.ShowConsist(Editor, SingleSelectedService!.Consist);
    }

    [RelayCommand(CanExecute = nameof(HasSingleSelectedService))]
    private async Task SaveConsist()
    {
        var consist = SingleSelectedService!.Consist;
        string? consistElement = null;

        await Loading.RunAsync(
            Strings.preparing_consist.CurrentValue,
            async token => (await Editor.GetSession(token)).GetConsistRailVehiclesXml(consist),
            xml => consistElement = xml,
            allowRetry: false);

        if (consistElement is null)
        {
            return;
        }

        var dialog = new SaveConsistViewModel
        {
            ConsistElement = consistElement,
            Name = consist.LocomotiveName,
            LocomotiveName = consist.LocomotiveName,
        };

        var savedConsist = await _dialogs.Show(dialog);

        if (savedConsist is null)
        {
            return;
        }

        PersistenceService.SaveConsist(savedConsist);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedServices))]
    private async Task ReplaceConsist()
    {
        var targets = SelectedServices.Select(service => service.Consist).ToList();
        var dialog = _dialogs.Create<ReplaceConsistViewModel>();
        var replacement = await _dialogs.Show(dialog);

        if (replacement is null)
        {
            return;
        }

        await Edit(session => session.ReplaceConsists(targets, replacement));
    }

    [RelayCommand(CanExecute = nameof(HasSelectedServices))]
    private async Task DeleteConsist()
    {
        var targets = SelectedServices.Select(service => service.Consist).ToList();
        var isBulkSelection = targets.Count > 1;
        var question = isBulkSelection ? Strings.confirm_delete_consists : Strings.confirm_delete_consist;
        var acceptLabel = isBulkSelection ? Strings.delete_consists : Strings.delete_consist;
        var summary = isBulkSelection
            ? string.Format(Strings.consists_selected.CurrentValue, targets.Count)
            : string.Format(Strings.consist_summary.CurrentValue, targets[0].ServiceName, targets[0].LocomotiveName);

        var confirmation = new ConfirmationDialogViewModel
        {
            AcceptLabel = acceptLabel.CurrentValue,
            Title = Strings.delete_consist.CurrentValue,
            BodyText = $"{question.CurrentValue}\n\n{summary}",
        };

        var isConfirmed = await _dialogs.Show(confirmation);

        if (!isConfirmed)
        {
            return;
        }

        await Edit(session => session.DeleteConsists(targets));
    }
}
