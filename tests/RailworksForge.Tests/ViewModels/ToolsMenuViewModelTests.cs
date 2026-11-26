using NSubstitute;

using RailworksForge.Core.Packaging;
using RailworksForge.Core.Serialization;
using RailworksForge.Services;
using RailworksForge.ViewModels;

namespace RailworksForge.Tests.ViewModels;

public class ToolsMenuViewModelTests
{
    private readonly ToolsActivity _tools = new();
    private readonly IStoragePickerService _picker = Substitute.For<IStoragePickerService>();
    private readonly ISerzFileService _serz = Substitute.For<ISerzFileService>();
    private readonly IPackageInstallService _packages = Substitute.For<IPackageInstallService>();
    private readonly ToolsMenuViewModel _toolsMenu;

    public ToolsMenuViewModelTests()
    {
        _toolsMenu = new ToolsMenuViewModel(_tools, _picker, _serz, _packages);
    }

    [Fact]
    public async Task ConvertBinToXml_ConvertsThePickedFile()
    {
        _picker.PickFile(Arg.Any<string>(), Arg.Any<FileTypeFilter>()).Returns("/scenario/Scenario.bin");

        await _toolsMenu.ConvertBinToXmlCommand.ExecuteAsync(null);

        await _serz.Received(1).ConvertBinToXml("/scenario/Scenario.bin", Arg.Any<CancellationToken>());
        Assert.False(_toolsMenu.Operations.IsLoading);
    }

    [Fact]
    public async Task ConvertBinToXml_OffersOnlyBinaryFiles()
    {
        await _toolsMenu.ConvertBinToXmlCommand.ExecuteAsync(null);

        await _picker.Received(1).PickFile(Arg.Any<string>(), Arg.Is<FileTypeFilter>(filter => filter.Patterns.SequenceEqual(new[] { "*.bin" })));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("/scenario/Scenario.xml")]
    public async Task ConvertBinToXml_WithoutABinaryFile_DoesNothing(string? pickedPath)
    {
        _picker.PickFile(Arg.Any<string>(), Arg.Any<FileTypeFilter>()).Returns(pickedPath);

        await _toolsMenu.ConvertBinToXmlCommand.ExecuteAsync(null);

        await _serz.DidNotReceiveWithAnyArgs().ConvertBinToXml(default!, default);
    }

    [Fact]
    public async Task ConvertXmlToBin_ConvertsThePickedFile()
    {
        _picker.PickFile(Arg.Any<string>(), Arg.Any<FileTypeFilter>()).Returns("/scenario/Scenario.xml");

        await _toolsMenu.ConvertXmlToBinCommand.ExecuteAsync(null);

        await _serz.Received(1).ConvertXmlToBin("/scenario/Scenario.xml", Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("/scenario/Scenario.bin")]
    public async Task ConvertXmlToBin_WithoutAnXmlFile_DoesNothing(string? pickedPath)
    {
        _picker.PickFile(Arg.Any<string>(), Arg.Any<FileTypeFilter>()).Returns(pickedPath);

        await _toolsMenu.ConvertXmlToBinCommand.ExecuteAsync(null);

        await _serz.DidNotReceiveWithAnyArgs().ConvertXmlToBin(default!, default);
    }

    [Fact]
    public async Task ConvertFailure_IsShownOnTheToolsOperation()
    {
        _picker.PickFile(Arg.Any<string>(), Arg.Any<FileTypeFilter>()).Returns("/scenario/Scenario.bin");
        _serz.ConvertBinToXml(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<string>(new IOException("Corrupt file")));

        await _toolsMenu.ConvertBinToXmlCommand.ExecuteAsync(null);

        Assert.Equal("Corrupt file", _toolsMenu.Operations.ErrorMessage);
    }

    [Fact]
    public async Task InstallPackage_InstallsThePickedPackagesAndClearsProgress()
    {
        List<string> files = ["/downloads/one.rwp", "/downloads/two.rpk"];
        _picker.PickFiles(Arg.Any<string>(), Arg.Any<FileTypeFilter>()).Returns(files);
        _tools.Progress.IsVisible = true;

        await _toolsMenu.InstallPackageCommand.ExecuteAsync(null);

        await _packages.Received(1).Install(files, Arg.Any<IProgress<InstallProgress>>());
        Assert.False(_tools.Progress.IsVisible);
        Assert.False(_toolsMenu.Operations.IsLoading);
    }

    [Fact]
    public async Task InstallPackage_WithNoFilesPicked_DoesNothing()
    {
        _picker.PickFiles(Arg.Any<string>(), Arg.Any<FileTypeFilter>()).Returns([]);

        await _toolsMenu.InstallPackageCommand.ExecuteAsync(null);

        await _packages.DidNotReceiveWithAnyArgs().Install(default!, default!);
    }

    [Fact]
    public async Task InstallPackage_WhileAnotherToolIsRunning_DoesNotInterruptIt()
    {
        var release = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _picker.PickFile(Arg.Any<string>(), Arg.Any<FileTypeFilter>()).Returns("/scenario/Scenario.bin");
        _picker.PickFiles(Arg.Any<string>(), Arg.Any<FileTypeFilter>()).Returns(["/downloads/one.rwp"]);
        _serz.ConvertBinToXml(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(release.Task);
        var conversion = _toolsMenu.ConvertBinToXmlCommand.ExecuteAsync(null);

        await _toolsMenu.InstallPackageCommand.ExecuteAsync(null);

        Assert.True(_toolsMenu.Operations.IsLoading);
        await _packages.DidNotReceiveWithAnyArgs().Install(default!, default!);

        release.SetResult("/scenario/Scenario.bin.xml");
        await conversion;
    }
}
