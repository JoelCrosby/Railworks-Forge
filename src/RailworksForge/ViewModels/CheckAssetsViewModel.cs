using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using Dameng.SepEx;

using DynamicData;

using nietras.SeparatedValues;

using RailworksForge.Core;
using RailworksForge.Core.Extensions;
using RailworksForge.Core.External;
using RailworksForge.Core.Models;
using RailworksForge.Core.Models.Common;

using ReactiveUI;

using Serilog;

namespace RailworksForge.ViewModels;

public partial class CheckAssetsViewModel : ViewModelBase
{
    [ObservableProperty]
    private Route _route;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _loadingProgress;

    [ObservableProperty]
    private string _loadingMessage;

    [ObservableProperty]
    private string _loadingStatusMessage;

    [ObservableProperty]
    private ObservableCollection<Blueprint> _blueprints;

    private readonly CancellationTokenSource _cts = new ();

    public CheckAssetsViewModel(Route route)
    {
        Route = route;
        IsLoading = true;
        Blueprints = [];
        LoadingMessage = string.Empty;
        LoadingStatusMessage = string.Empty;

        if (Design.IsDesignMode)
        {
            return;
        }

        Observable.Start(RunAssetCheck, RxApp.TaskpoolScheduler);
    }

    private async Task RunAssetCheck()
    {
        var blueprints = await GetBlueprints();
        var missing = await GetMissingAssets(blueprints);

        Dispatcher.UIThread.Post(() =>
        {
            Blueprints.AddRange(missing);
            IsLoading = false;
        });
    }

    private async Task CacheBlueprintResults(IEnumerable<Blueprint> blueprints)
    {
        var path = Paths.GetRouteAssetsCachePath(Route);
        var directory = Directory.GetParent(path)?.FullName;

        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        await using var writer = Sep.Writer().ToFile(path);

        writer.WriteRecords(blueprints);
    }

    private List<Blueprint> GetCachedBlueprintResults()
    {
        var path = Paths.GetRouteAssetsCachePath(Route);

        using var reader = Sep.Reader().FromFile(path);
        return reader.GetRecords<Blueprint>().ToList();
    }

    private bool HasCachedBlueprints()
    {
        var path = Paths.GetRouteAssetsCachePath(Route);
        return Paths.Exists(path);
    }

    private async Task<List<Blueprint>> GetBlueprints()
    {
        if (HasCachedBlueprints())
        {
            return GetCachedBlueprintResults();
        }

        var sceneryBinFiles = GetBinFiles("Scenery", true);
        var networkBinFiles = GetBinFiles("Networks", false);

        var binFiles = sceneryBinFiles.Concat(networkBinFiles).ToList();

        var blueprintDictionary = await GetBlueprintsFromBinaries(binFiles);
        var blueprints = blueprintDictionary.Keys.ToList();

        await CacheBlueprintResults(blueprints);

        return blueprints;
    }

    private async Task<ConcurrentDictionary<Blueprint, byte>> GetBlueprintsFromBinaries(List<string> binFiles)
    {
        var results = new ConcurrentDictionary<Blueprint, byte>();

        var processedCount = 0;
        var amountToProcess = binFiles.Count;

        await Parallel.ForEachAsync(binFiles, _cts.Token, async (path, token) =>
        {
            try
            {
                var serialised = await Serz.Convert(path, token);
                var xml = File.OpenRead(serialised.OutputPath);

                // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                using var document = XmlParser.ParseDocument(xml);

                var entities = document.QuerySelectorAll("cDynamicEntity BlueprintID");

                var blueprints = entities.Select(el => new Blueprint
                {
                    BlueprintSetIdProvider = el.SelectTextContent("iBlueprintLibrary-cAbsoluteBlueprintID Provider"),
                    BlueprintSetIdProduct = el.SelectTextContent("iBlueprintLibrary-cAbsoluteBlueprintID Product"),
                    BlueprintId = el.SelectTextContent("iBlueprintLibrary-cAbsoluteBlueprintID BlueprintID"),
                });

                foreach (var blueprint in blueprints)
                {
                    if (string.IsNullOrWhiteSpace(blueprint.BlueprintId))
                    {
                        continue;
                    }

                    results.TryAdd(blueprint, byte.MinValue);
                }

                processedCount++;

                var count = processedCount;

                LoadingProgress = (int) Math.Ceiling((double)(100 * count) / amountToProcess);
                LoadingMessage = $"Processed {count} of {amountToProcess} files ( %{LoadingProgress} )";
                LoadingStatusMessage = $"Processing path: {path}";
            }
            catch (Exception e)
            {
                Log.Error(e, "check assets for path path failed");
            }
        });

        return results;
    }

    private async Task<List<Blueprint>> GetMissingAssets(List<Blueprint> blueprints)
    {
        var amountCheckedCount = 0;
        var amountToCheck = blueprints.Count;

        var missing = await Observable.Start(() =>
        {
            var notFound = new List<Blueprint>();

            foreach (var blueprint in blueprints)
            {
                amountCheckedCount++;

                var count = amountCheckedCount;

                Dispatcher.UIThread.Post(() =>
                {
                    LoadingProgress = (int) Math.Ceiling((double)(100 * count) / amountToCheck);
                    LoadingMessage = $"Checked {count} of {amountToCheck} blueprints ( %{LoadingProgress} )";
                    LoadingStatusMessage = $"Processing blueprint at path: {blueprint.BinaryPath}";
                });

                if (blueprint.AcquisitionState is not AcquisitionState.Found)
                {
                    notFound.Add(blueprint);
                }
            }

            return notFound.OrderBy(n => n.BlueprintSetIdProvider).ToList();

        }, RxApp.TaskpoolScheduler);

        return missing;
    }

    private List<string> GetBinFiles(string directory, bool allDirectories)
    {
        var absolutePath = Path.Join(Route.DirectoryPath, directory);
        var searchOption =  allDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        if (Paths.Exists(absolutePath))
        {
            return Directory.EnumerateFiles(absolutePath, "*.bin", searchOption).ToList();
        }

        var archivePath = Route.MainContentArchivePath;

        if (Paths.Exists(archivePath) is false)
        {
            throw new Exception($"Could not find archive at {archivePath}");
        }

        Archives.ExtractDirectory(archivePath, directory);

        return Directory.EnumerateFiles(absolutePath, "*.bin", searchOption).ToList();
    }

    public void OnClose()
    {
        _cts.Cancel();
    }
}
