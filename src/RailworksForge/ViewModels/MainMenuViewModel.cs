using System;
using System.IO;
using System.Reactive;

using RailworksForge.Core;
using RailworksForge.Core.External;
using RailworksForge.Util;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public class MainMenuViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, Unit> ExitCommand { get; } = ReactiveCommand.Create(() => Environment.Exit(0));

    public ReactiveCommand<Unit, Unit> ConvertBinToXmlCommand { get; }
    public ReactiveCommand<Unit, Unit> ConvertXmlToBinCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenSettingsDirectoryCommand { get; }

    public MainMenuViewModel()
    {
        ConvertBinToXmlCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var storageFile = await Utils.OpenFilePickerAsync("Select .bin file");

            if (storageFile is null) return;

            var path = storageFile.Path.AbsolutePath;

            if (Path.GetExtension(path) != ".bin") return;

            await Serz.Convert(path);
        });

        ConvertXmlToBinCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var storageFile = await Utils.OpenFilePickerAsync("Select .bin file");

            if (storageFile is null) return;

            var path = storageFile.Path.AbsolutePath;

            if (Path.GetExtension(path) != ".xml") return;

            await Serz.Convert(path);
        });

        OpenSettingsDirectoryCommand = ReactiveCommand.Create(() =>
        {
            Launcher.Open(Paths.GetConfigurationFolder());
        });
    }
}
