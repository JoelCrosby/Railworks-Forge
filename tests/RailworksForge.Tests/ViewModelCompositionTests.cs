using RailworksForge.Core.Consists;
using RailworksForge.Core.Scenarios;
using RailworksForge.DesignData;
using RailworksForge.Tests.Components;
using RailworksForge.ViewModels;

namespace RailworksForge.Tests;

// Pages and dialogs are built by ActivatorUtilities at runtime, so a missing registration only fails when a user opens them.
// Resolving them reads Dispatcher.UIThread, which must belong to the shared headless session's thread.
[Collection(ComponentSession.CollectionName)]
public class ViewModelCompositionTests
{
    [Fact]
    public Task ShellViewModels_ResolveFromServices()
    {
        return ComponentSession.Run(() =>
        {
            Assert.NotNull(DesignServices.Get<MainWindowViewModel>());
            Assert.NotNull(DesignServices.Get<RoutesViewModel>());
            Assert.NotNull(DesignServices.Get<SettingsViewModel>());
        });
    }

    [Fact]
    public Task PagesAndDialogs_CanBeCreatedWithRuntimeArguments()
    {
        return ComponentSession.Run(AssertPagesAndDialogsCanBeCreated);
    }

    private static void AssertPagesAndDialogsCanBeCreated()
    {
        Assert.NotNull(DesignViewModels.NavigationBar);
        Assert.NotNull(DesignViewModels.RouteDetail);
        Assert.NotNull(DesignViewModels.ScenarioDetail);
        Assert.NotNull(DesignViewModels.ReplaceConsist);
        Assert.NotNull(DesignViewModels.CheckAssets);
        Assert.NotNull(DesignViewModels.ReplaceTrack);

        var consist = new Consist
        {
            Id = "1",
            LocomotiveName = "Class 390",
            ServiceName = "1A01",
            ServiceId = "1A01",
            Vehicles = [],
            BlueprintSetIdProvider = "DTG",
            BlueprintSetIdProduct = "WCML",
            BlueprintId = @"RailVehicles\Class390.xml",
        };

        Assert.NotNull(DesignServices.Create<ConsistDetailViewModel>(new ScenarioEditor(DesignData.DesignData.Scenario), consist));
    }
}
