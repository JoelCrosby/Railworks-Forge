using System.Linq;

using RailworksForge.Core.Models.Examples;

namespace RailworksForge.ViewModels;

public class DesignSaveConsistViewModel : SaveConsistViewModel
{
    public DesignSaveConsistViewModel()
    {
        Consist = Example.Scenario.Consists.First();
    }
}
