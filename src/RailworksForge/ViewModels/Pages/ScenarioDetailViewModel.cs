using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using RailworksForge.Core;
using RailworksForge.Core.Models;
using RailworksForge.Services;
using RailworksForge.Util;

namespace RailworksForge.ViewModels;

public partial class ScenarioDetailViewModel : ViewModelBase
{
    private readonly ScenarioService _scenarioService;
    private readonly ConsistEditService _consistEdits;
    private readonly NavigationService _navigation;
    private readonly DialogService _dialogs;
    private readonly LauncherService _launcher;
    private readonly ImageService _images;

    [ObservableProperty]
    public partial Scenario Scenario { get; set; }

    [ObservableProperty]
    public partial string? SearchTerm { get; set; }

    public SearchableCollection<ConsistViewModel> Services { get; } = new(service => service.Consist.SearchIndex);

    public ObservableCollection<ConsistViewModel> SelectedServices { get; } = [];

    public ScenarioDetailViewModel(
        Scenario scenario,
        ScenarioService scenarioService,
        ConsistEditService consistEdits,
        NavigationService navigation,
        DialogService dialogs,
        LauncherService launcher,
        ImageService images)
    {
        Scenario = scenario;
        _scenarioService = scenarioService;
        _consistEdits = consistEdits;
        _navigation = navigation;
        _dialogs = dialogs;
        _launcher = launcher;
        _images = images;

        SelectedServices.CollectionChanged += (_, _) => NotifySelectionCommands();
    }

    private ConsistViewModel? SingleSelectedService => SelectedServices.Count is 1 ? SelectedServices[0] : null;

    private bool HasSingleSelectedService => SingleSelectedService is not null;

    private bool HasSelectedServices => SelectedServices.Count > 0;

    protected override Task OnActivated()
    {
        return LoadServices();
    }

    partial void OnSearchTermChanged(string? value)
    {
        Services.Filter(value);
    }

    private Task LoadServices()
    {
        var scenario = Scenario;

        return Loading.RunAsync("Loading scenario services…", async token =>
        {
            var loaded = await _scenarioService.LoadConsists(scenario, token);
            var services = loaded.Consists.Select(consist => new ConsistViewModel(consist)).ToList();

            foreach (var service in services)
            {
                token.ThrowIfCancellationRequested();
                service.ImageBitmap = _images.GetConsistImage(service.Consist);
            }

            return (loaded.Scenario, Services: services);
        }, result =>
        {
            Scenario = result.Scenario;
            Services.Reset(result.Services);
        });
    }

    private async Task ReloadAfterEdit()
    {
        var shouldReload = !Loading.HasError && IsActive;

        if (shouldReload)
        {
            await LoadServices();
        }
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
        _launcher.OpenDirectory(Scenario.DirectoryPath);
    }

    [RelayCommand]
    private void OpenBackupsFolder()
    {
        _launcher.OpenDirectory(Scenario.BackupDirectory);
    }

    [RelayCommand]
    private Task ExportBinToXml()
    {
        return Loading.RunAsync("Exporting scenario XML…", _ => Scenario.ExportBinToXml(), path =>
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
        return Loading.RunAsync("Converting scenario XML…", _ => Scenario.ConvertXmlToBin());
    }

    [RelayCommand]
    private Task ExtractScenarios()
    {
        return Loading.RunAsync("Extracting scenarios…", _ =>
        {
            Scenario.Route.ExtractScenarios();

            return Task.CompletedTask;
        });
    }

    [RelayCommand(CanExecute = nameof(HasSingleSelectedService))]
    private void OpenService()
    {
        _navigation.ShowConsist(Scenario, SingleSelectedService!.Consist);
    }

    [RelayCommand(CanExecute = nameof(HasSingleSelectedService))]
    private async Task SaveConsist()
    {
        var consist = SingleSelectedService!.Consist;
        string? consistElement = null;

        await Loading.RunAsync(
            "Preparing consist…",
            _ => _scenarioService.GetConsistRailVehiclesXml(Scenario, consist),
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

        await Loading.RunAsync("Updating scenario…", _ => _consistEdits.ReplaceConsists(Scenario, targets, replacement));
        await ReloadAfterEdit();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedServices))]
    private async Task DeleteConsist()
    {
        var targets = SelectedServices.Select(service => service.Consist).ToList();
        var isBulkSelection = targets.Count > 1;
        var consistLabel = isBulkSelection ? "consists" : "consist";
        var summary = isBulkSelection
            ? $"{targets.Count} consists selected."
            : $"Consist: {targets[0].ServiceName} - {targets[0].LocomotiveName}";

        var confirmation = new ConfirmationDialogViewModel
        {
            AcceptLabel = $"Delete {consistLabel}",
            Title = "Delete Consist",
            BodyText = $"""
                        Are you sure you wish to delete the selected {consistLabel}?

                        {summary}
                        """,
        };

        var isConfirmed = await _dialogs.Show(confirmation);

        if (!isConfirmed)
        {
            return;
        }

        await Loading.RunAsync("Updating scenario…", _ => _consistEdits.DeleteConsists(Scenario, targets));
        await ReloadAfterEdit();
    }
}
