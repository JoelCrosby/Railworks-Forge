using System;
using System.IO;
using System.Reactive;


using RailworksForge.Core;
using RailworksForge.Core.External;
using RailworksForge.Core.Packaging;
using RailworksForge.Util;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public class MainMenuViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, Unit> ExitCommand { get; } = ReactiveCommand.Create(() => Environment.Exit(0));

    public ReactiveCommand<Unit, Unit> ConvertBinToXmlCommand { get; }
    public ReactiveCommand<Unit, Unit> ConvertXmlToBinCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenSettingsDirectoryCommand { get; }
    public ReactiveCommand<Unit, Unit> InstallPackageCommand { get; }

    public MainMenuViewModel()
    {
        ConvertBinToXmlCommand = ReactiveCommand.CreateFromTask(async (token) =>
        {
            var storageFile = await Utils.OpenFilePickerAsync("Select .bin file");

            if (storageFile is null) return;

            var path = storageFile.Path.LocalPath;

            if (Path.GetExtension(path) != ".bin") return;

            var result = await Serz.Convert(path, token, true);

            File.Copy(result.OutputPath, path.Replace(".bin", ".bin.xml"));
        });

        ConvertXmlToBinCommand = ReactiveCommand.CreateFromTask(async (token) =>
        {
            var storageFile = await Utils.OpenFilePickerAsync("Select .bin file");

            if (storageFile is null) return;

            var path = storageFile.Path.LocalPath;

            if (Path.GetExtension(path) != ".xml") return;

            var result = await Serz.Convert(path, token, true);

            File.Copy(result.OutputPath, path.Replace(".bin.xml", ".bin"));
        });

        OpenSettingsDirectoryCommand = ReactiveCommand.Create(() =>
        {
            Launcher.Open(Paths.GetConfigurationFolder());
        });

        InstallPackageCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var files = await Utils.OpenMultiFilePickerAsync("Select .rwp or .rpk file");

            if (files.Count is 0) return;

            var packager = new Packager();
            var mainWindow = Utils.GetApplicationViewModel();

            packager.PackageInstallProgressSubject.Subscribe(args =>
            {
                mainWindow.UpdateProgressIndicator(args);
            });

            foreach (var file in files)
            {
                await packager.InstallPackage(file.Path.LocalPath);
            }

            mainWindow.ClearProgressIndicator();
        });
    }
}
