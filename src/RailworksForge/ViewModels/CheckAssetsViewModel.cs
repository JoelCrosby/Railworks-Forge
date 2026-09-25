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

using nietras.SeparatedValues;

using RailworksForge.Core;
using RailworksForge.Core.Extensions;
using RailworksForge.Core.External;
using RailworksForge.Core.Models;
using RailworksForge.Util;
using RailworksForge.Core.Models.Common;

using ReactiveUI;

using Serilog;

namespace RailworksForge.ViewModels;

public partial class CheckAssetsViewModel : ViewModelBase
{
    [ObservableProperty]
    private Route _route;

    [ObservableProperty]
    private int _loadingProgress;

    [ObservableProperty]
    private string _loadingMessage;

    [ObservableProperty]
    private string _loadingStatusMessage;

    [ObservableProperty]
    private ObservableCollection<Blueprint> _blueprints;



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

        _ = RunAssetCheck();
    }

    private Task RunAssetCheck()
    {
        return Loading.RunAsync("Checking route assets…", async token =>
        {
            var blueprints = await GetBlueprints(token);
            var missing = GetMissingAssets(blueprints, token);

            return missing;
        }, missing => Blueprints = new ObservableCollection<Blueprint>(missing));
    }

    private void ReportProgress(int percentage, string message, CancellationToken token)
    {
        Dispatcher.UIThread.Post(() =>
        {

            if (!token.IsCancellationRequested)
            {
                LoadingProgress = percentage;
                LoadingMessage = message;
            }
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

    private async Task<List<Blueprint>> GetBlueprints(CancellationToken token)
    {
        if (HasCachedBlueprints())
        {
            return GetCachedBlueprintResults();
        }

        var sceneryBinFiles = GetBinFiles("Scenery", true);
        var networkBinFiles = GetBinFiles("Networks", false);

        var binFiles = sceneryBinFiles.Concat(networkBinFiles).ToList();

        var blueprintDictionary = await GetBlueprintsFromBinaries(binFiles, token);
        var blueprints = blueprintDictionary.Keys.ToList();

        await CacheBlueprintResults(blueprints);

        return blueprints;
    }

    private async Task<ConcurrentDictionary<Blueprint, byte>> GetBlueprintsFromBinaries(List<string> binFiles, CancellationToken cancellationToken)
    {
        var results = new ConcurrentDictionary<Blueprint, byte>();

        var processedCount = 0;
        var amountToProcess = binFiles.Count;

        var options = new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = 4 };
        var lastPercentage = -1;

        await Parallel.ForEachAsync(binFiles, options, async (path, token) =>
        {
            try
            {
                var serialised = await Serz.Convert(path, token);
                using var xml = File.OpenRead(serialised.OutputPath);

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

                var count = Interlocked.Increment(ref processedCount);
                var percentage = (int)(100L * count / amountToProcess);

                if (Interlocked.Exchange(ref lastPercentage, percentage) != percentage)
                {
                    ReportProgress(percentage, $"Processed {count} of {amountToProcess} files", token);
                }
            }
            catch (Exception e)
            {
                throw new IOException($"Unable to check {path}", e);
            }
        });

        return results;
    }

    private List<Blueprint> GetMissingAssets(List<Blueprint> blueprints, CancellationToken token)
    {
        var notFound = new List<Blueprint>();
        var lastPercentage = -1;

        for (var index = 0; index < blueprints.Count; index++)
        {
            token.ThrowIfCancellationRequested();
            var blueprint = blueprints[index];

            if (blueprint.AcquisitionState is not AcquisitionState.Found)
            {
                notFound.Add(blueprint);
            }

            var count = index + 1;
            var percentage = (int)(100L * count / blueprints.Count);

            if (percentage != lastPercentage)
            {
                lastPercentage = percentage;
                ReportProgress(percentage, $"Checked {count} of {blueprints.Count} blueprints", token);
            }
        }

        return notFound.OrderBy(blueprint => blueprint.BlueprintSetIdProvider).ToList();
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
        CancelLoading();
    }
}
