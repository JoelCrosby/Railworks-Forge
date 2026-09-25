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
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

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

    public LoadingOperation StockLoading { get; } = new();

    public required Scenario Scenario { get; init; }

    [ObservableProperty]
    private BrowserDirectory? _selectedDirectory;

    [ObservableProperty]
    private ObservableCollection<BrowserDirectory> _directoryTree;

    public ObservableCollection<PreloadConsistViewModel> PreloadConsists { get; }

    [ObservableProperty]
    private PreloadConsistViewModel? _selectedConsist;

    public ReplaceConsistViewModel()
    {
        PreloadConsists = [];
        DirectoryTree = [];

        if (!Design.IsDesignMode)
        {
            _ = Loading.RunAsync("Loading asset providers…",
                _ => Task.FromResult(BrowserDirectory.ViewAllBrowser().ToList()),
                items => DirectoryTree = new ObservableCollection<BrowserDirectory>(items));
        }

        ReplaceConsistCommand = ReactiveCommand.Create(() => SelectedConsist?.Consist);
        LoadAvailableStockCommand = ReactiveCommand.CreateFromTask(LoadAvailableStock);
        OpenInExplorerCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedDirectory is null) return;

            Launcher.Open(SelectedDirectory.AssetDirectory.Path);
        });

    }

    public override void CancelLoading()
    {
        base.CancelLoading();
        StockLoading.Cancel();
    }

    partial void OnSelectedDirectoryChanged(BrowserDirectory? value)
    {
        StockLoading.Cancel();
        PreloadConsists.Clear();
        SelectedConsist = null;
    }

    private Task LoadAvailableStock()
    {
        var directory = SelectedDirectory;
        PreloadConsists.Clear();

        if (directory is null)
        {
            return Task.CompletedTask;
        }

        return StockLoading.RunAsync("Loading replacement consists…", async token =>
        {
            var preloadDirectory = GetPreloadDirectory(directory);

            if (preloadDirectory is null || !Paths.Exists(preloadDirectory))
            {
                return new List<PreloadConsistViewModel>();
            }

            var binFiles = Directory.EnumerateFiles(preloadDirectory, "*.bin", SearchOption.AllDirectories);
            var results = new List<PreloadConsistViewModel>();

            foreach (var binFile in binFiles)
            {
                token.ThrowIfCancellationRequested();
                var exported = await Serz.Convert(binFile, token);
                var consists = await GetConsistBlueprints(exported.OutputPath, token);
                var models = consists.ConvertAll(consist => new PreloadConsistViewModel(consist));
                LoadImages(models);
                results.AddRange(models);
            }

            return results;
        }, models => PreloadConsists.AddRange(models));
    }

    private static void LoadImages(IEnumerable<PreloadConsistViewModel> items)
    {
        try
        {
            foreach (var item in items)
            {
                item.LoadImage();
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

        return Directory.GetDirectories(directory.AssetDirectory.Path)
            .FirstOrDefault(path => path.Contains("PreLoad", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<List<PreloadConsist>> GetConsistBlueprints(string path, CancellationToken cancellationToken = default)
    {
        using var file = File.OpenRead(path);
        using var doc = await XmlParser.ParseDocumentAsync(file, cancellationToken);

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
                    using var fragmentDocument = await parsed.Blueprint.GetXmlDocument();
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
