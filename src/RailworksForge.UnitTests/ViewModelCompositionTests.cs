using RailworksForge.Core.Models;
using RailworksForge.DesignData;
using RailworksForge.ViewModels;

namespace RailworksForge.UnitTests;

// Pages and dialogs are built by ActivatorUtilities at runtime, so a missing registration only fails when a user opens them.
public class ViewModelCompositionTests
{
    [Fact]
    public void ShellViewModels_ResolveFromServices()
    {
        Assert.NotNull(DesignServices.Get<MainWindowViewModel>());
        Assert.NotNull(DesignServices.Get<RoutesViewModel>());
    }

    [Fact]
    public void PagesAndDialogs_CanBeCreatedWithRuntimeArguments()
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

        Assert.NotNull(DesignServices.Create<ConsistDetailViewModel>(DesignData.DesignData.Scenario, consist));
    }
}
