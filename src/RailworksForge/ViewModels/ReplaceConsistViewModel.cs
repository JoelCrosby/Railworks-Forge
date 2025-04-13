using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;

using AngleSharp.Dom;

using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using DynamicData;

using RailworksForge.Core;
using RailworksForge.Core.External;
using RailworksForge.Core.Models;
using RailworksForge.Util;

using ReactiveUI;

using Serilog;

namespace RailworksForge.ViewModels;

public partial class ReplaceConsistViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, PreloadConsist?> ReplaceConsistCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadAvailableStockCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenInExplorerCommand { get; }

    public required Scenario Scenario { get; init; }

    [ObservableProperty]
    private BrowserDirectory? _selectedDirectory;

    [ObservableProperty]
    private ObservableCollection<BrowserDirectory> _directoryTree;

    private ObservableCollection<PreloadConsistViewModel> PreloadConsists { get; }
    public FlatTreeDataGridSource<PreloadConsistViewModel> PreloadConsistsSource { get; }

    public PreloadConsistViewModel? SelectedConsist => PreloadConsistsSource.RowSelection?.SelectedItem;

    public ReplaceConsistViewModel()
    {
        PreloadConsists = [];
        DirectoryTree = new ObservableCollection<BrowserDirectory>(BrowserDirectory.ViewAllBrowser());

        ReplaceConsistCommand = ReactiveCommand.Create(() => SelectedConsist?.Consist);
        LoadAvailableStockCommand = ReactiveCommand.CreateFromTask(LoadAvailableStock);
        OpenInExplorerCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedDirectory is null) return;

            Launcher.Open(SelectedDirectory.AssetDirectory.Path);
        });

        PreloadConsistsSource = new FlatTreeDataGridSource<PreloadConsistViewModel>(PreloadConsists)
        {
            Columns =
            {
                new TemplateColumn<PreloadConsistViewModel>("Image", "ImageCell"),
                new TextColumn<PreloadConsistViewModel, string>("Locomotive Name", x => x.Consist.LocomotiveName),
                new TextColumn<PreloadConsistViewModel, string>("Display Name", x => x.Consist.DisplayName),
                new TextColumn<PreloadConsistViewModel, LocoClass>("Engine Type", x => x.Consist.EngineType),
                new TextColumn<PreloadConsistViewModel, int>("Length", x => x.Consist.ConsistEntries.Count),
            },
        };
    }

    private async Task LoadAvailableStock()
    {
        Dispatcher.UIThread.Post(PreloadConsists.Clear);

        if (SelectedDirectory is null) return;

        var preloadDirectory = GetPreloadDirectory(SelectedDirectory);

        if (preloadDirectory is null || !Paths.Exists(preloadDirectory)) return;

        var binFiles = Directory
            .EnumerateFiles(preloadDirectory, "*.bin", SearchOption.AllDirectories)
            .Where(f => f.Equals("MetaData.bin", StringComparison.OrdinalIgnoreCase) is not true);

        await Parallel.ForEachAsync(binFiles, async (binFile, cancellationToken) =>
        {
            var exported = await Serz.Convert(binFile, cancellationToken);
            var consists = await GetConsistBlueprints(exported.OutputPath, cancellationToken);
            var models = consists.ConvertAll(c => new PreloadConsistViewModel(c));

            LoadImages(models);

            Dispatcher.UIThread.Post(() => PreloadConsists.AddRange(models));
        });
    }

    private static async void LoadImages(IEnumerable<PreloadConsistViewModel> items)
    {
        try
        {
            foreach (var item in items)
            {
                await item.LoadImage();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "an error occured while trying to load stock images");
        }
    }

    private static string? GetPreloadDirectory(BrowserDirectory directory)
    {
        var assetDirectories = Directory.GetDirectories(directory.AssetDirectory.Path);
        var preloadDirectory = assetDirectories.FirstOrDefault(d => d.Contains("PreLoad", StringComparison.OrdinalIgnoreCase));

        if (preloadDirectory is not null && Paths.Exists(preloadDirectory))
        {
            return preloadDirectory;
        }

        foreach (var package in Directory.EnumerateFiles(directory.AssetDirectory.Path, "*.ap"))
        {
            Archives.ExtractDirectory(package, "PreLoad");
        }

        return preloadDirectory;
    }

    private static async Task<List<PreloadConsist>> GetConsistBlueprints(string path, CancellationToken cancellationToken = default)
    {
        var file = File.OpenRead(path);
        var doc = await XmlParser.ParseDocumentAsync(file, cancellationToken);

        var blueprints = doc.QuerySelectorAll("Blueprint cConsistBlueprint").ToList();

        var consists = new List<PreloadConsist>();

        await Inner(blueprints);

        return consists;

        async Task Inner(List<IElement> elements)
        {
            foreach (var element in elements)
            {
                var parsed = PreloadConsist.Parse(element);

                if (parsed is null) continue;

                if (parsed.Blueprint.BlueprintId.Contains("fragment", StringComparison.OrdinalIgnoreCase))
                {
                    var fragmentDocument = await parsed.Blueprint.GetXmlDocument();
                    var fragmentBlueprints = fragmentDocument.QuerySelectorAll("Blueprint cConsistFragmentBlueprint").ToList();

                    await Inner(fragmentBlueprints);
                }
                else
                {
                    consists.Add(parsed);
                }
            }
        }
    }
}
