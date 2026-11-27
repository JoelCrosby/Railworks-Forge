using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using RailworksForge.Core.Consists;
using RailworksForge.Core.Scenarios;
using RailworksForge.Services;
using RailworksForge.Translations;
using RailworksForge.Util;

namespace RailworksForge.ViewModels;

public partial class ScenarioDetailViewModel : ViewModelBase
{
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;
    private readonly ILauncherService _launcher;
    private readonly IImageService _images;
    private readonly IScenarioService _scenarios;
    private readonly ISavedConsistService _savedConsists;
    private readonly BackgroundImageLoader _imageLoader = new();

    [ObservableProperty]
    public partial Scenario Scenario { get; private set; }

    [ObservableProperty]
    public partial string? SearchTerm { get; set; }

    [ObservableProperty]
    public partial Bitmap? HeaderImage { get; private set; }

    public SearchableCollection<ConsistViewModel> Services { get; } = new(service => service.Consist.SearchIndex);

    public ObservableCollection<ConsistViewModel> SelectedServices { get; } = [];

    public IScenarioEditor Editor { get; }

    public PendingChangesViewModel PendingChanges { get; }

    public int SelectedCount => SelectedServices.Count;

    public string SelectionText => string.Format(Strings.items_selected.CurrentValue, SelectedCount);

    public override string StatusText
    {
        get
        {
            var services = string.Format(Strings.service_count.CurrentValue, Services.Count);

            return SelectedCount > 0
                ? string.Format(Strings.status_items_with_selection.CurrentValue, services, SelectedCount)
                : services;
        }
    }

    public ScenarioDetailViewModel(
        Scenario scenario,
        INavigationService navigation,
        IDialogService dialogs,
        ILauncherService launcher,
        IImageService images,
        IScenarioService scenarios,
        ISavedConsistService savedConsists)
    {
        Scenario = scenario;
        _navigation = navigation;
        _dialogs = dialogs;
        _launcher = launcher;
        _images = images;
        _scenarios = scenarios;
        _savedConsists = savedConsists;

        Editor = scenarios.CreateEditor(scenario);
        PendingChanges = new PendingChangesViewModel(Editor);

        SelectedServices.CollectionChanged += (_, _) => OnSelectionChanged();
        Editor.PropertyChanged += (_, _) => NotifyEditCommands();
    }

    private ConsistViewModel? SingleSelectedService => SelectedServices.Count is 1 ? SelectedServices[0] : null;

    private bool HasSingleSelectedService => SingleSelectedService is not null;

    private bool HasSelectedServices => SelectedServices.Count > 0;

    protected override Task OnActivated()
    {
        PendingChanges.Attach();

        return LoadServices();
    }

    protected override void OnDeactivated()
    {
        _imageLoader.Cancel();
        PendingChanges.Detach();
    }

    partial void OnSearchTermChanged(string? value)
    {
        Services.Filter(value);
        OnPropertyChanged(nameof(StatusText));
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
    private Task Edit(Func<IScenarioEditSession, Task> edit)
    {
        return Loading.RunAsync(Strings.updating_scenario.CurrentValue, async token =>
        {
            var session = await Editor.GetSession(token);
            await edit(session);

            return GetServices(session, token);
        }, ShowServices, allowRetry: false);
    }

    private static List<ConsistViewModel> GetServices(IScenarioEditSession session, CancellationToken token)
    {
        return session.GetConsists(token).Select(consist => new ConsistViewModel(consist)).ToList();
    }

    private void ShowServices(List<ConsistViewModel> services)
    {
        Editor.Update();
        Scenario = Editor.Scenario;
        Services.Reset(services);
        OnPropertyChanged(nameof(StatusText));
        _imageLoader.Load(
            services,
            service => _images.GetConsistImage(service.Consist),
            ShowServiceImage);
    }

    private void ShowServiceImage(ConsistViewModel service, Bitmap image)
    {
        service.ImageBitmap = image;

        if (service.Consist.PlayerDriver)
        {
            HeaderImage = image;
        }
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

    private void OnSelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(SelectionText));
        OnPropertyChanged(nameof(StatusText));
        NotifySelectionCommands();
    }

    private void NotifySelectionCommands()
    {
        OpenServiceCommand.NotifyCanExecuteChanged();
        SaveConsistCommand.NotifyCanExecuteChanged();
        ReplaceConsistCommand.NotifyCanExecuteChanged();
        DeleteConsistCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void ClearSelection()
    {
        SelectedServices.Clear();
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
        return Loading.RunAsync(Strings.exporting_scenario_xml.CurrentValue, _ => _scenarios.ExportBinToXml(Scenario), path =>
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
        return Loading.RunAsync(Strings.converting_scenario_xml.CurrentValue, _ => _scenarios.ConvertXmlToBin(Scenario));
    }

    [RelayCommand]
    private Task ExtractScenarios()
    {
        return Loading.RunAsync(Strings.extracting_scenarios.CurrentValue, _ =>
        {
            _scenarios.ExtractScenarios(Scenario.Route);

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
        var dialog = new SaveConsistViewModel
        {
            Name = consist.LocomotiveName,
            LocomotiveName = consist.LocomotiveName,
        };

        var name = await _dialogs.Show(dialog);

        if (name is null)
        {
            return;
        }

        await Loading.RunAsync(
            Strings.saving_consist.CurrentValue,
            token => _savedConsists.Save(Editor, consist, name, token));
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
