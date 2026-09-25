using System;
using System.IO;
using System.Reactive;

using RailworksForge.Core.External;
using RailworksForge.Core.Packaging;
using RailworksForge.Util;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public class ToolbarViewModel : ViewModelBase
{
    public LoadingOperation Operations => Avalonia.Controls.Design.IsDesignMode
        ? Loading
        : Utils.GetApplicationViewModel().ToolsLoading;

    public ReactiveCommand<Unit, Unit> ConvertBinToXmlCommand { get; } = ReactiveCommand.CreateFromTask(async (token) =>
    {
        var storageFile = await Utils.OpenFilePickerAsync("Select .bin file");

        if (storageFile is null) return;

        var path = storageFile.Path.LocalPath;
        var extension = Path.GetExtension(path);

        if (extension is not ".bin") return;

        var loading = Utils.GetApplicationViewModel().ToolsLoading;

        if (loading.IsLoading)
        {
            return;
        }
        await loading.RunAsync("Converting file…", async cancellationToken =>
        {
            var result = await Serz.Convert(path, cancellationToken, true);
            File.Copy(result.OutputPath, path.Replace(extension, $"{extension}.xml"));
        });
    });

    public ReactiveCommand<Unit, Unit> ConvertXmlToBinCommand { get; } = ReactiveCommand.CreateFromTask(async (token) =>
    {
        var storageFile = await Utils.OpenFilePickerAsync("Select .bin file");

        if (storageFile is null) return;

        var path = storageFile.Path.LocalPath;
        var extension = Path.GetExtension(path);

        if (extension is not ".xml") return;

        var loading = Utils.GetApplicationViewModel().ToolsLoading;

        if (loading.IsLoading)
        {
            return;
        }
        await loading.RunAsync("Converting file…", async cancellationToken =>
        {
            var result = await Serz.Convert(path, cancellationToken, true);
            File.Copy(result.OutputPath, path.Replace($"{extension}.xml", extension));
        });
    });

    public ReactiveCommand<Unit, Unit> InstallPackageCommand { get; } = ReactiveCommand.CreateFromTask(async () =>
    {
        var files = await Utils.OpenMultiFilePickerAsync("Select .rwp or .rpk file");

        if (files.Count is 0) return;

        var packager = new Packager();
        var mainWindow = Utils.GetApplicationViewModel();

        using var subscription = packager.PackageInstallProgressSubject.Subscribe(args =>
        {
            mainWindow.UpdateProgressIndicator(args);
        });

        try
        {
            await mainWindow.ToolsLoading.RunAsync("Installing packages…", async _ =>
            {
                foreach (var file in files)
                {
                    await packager.InstallPackage(file.Path.LocalPath);
                }
            });
        }
        finally
        {
            mainWindow.ClearProgressIndicator();
        }
    });
}
