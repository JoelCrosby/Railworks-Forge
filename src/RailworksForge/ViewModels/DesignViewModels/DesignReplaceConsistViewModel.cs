using System.Linq;

using RailworksForge.Core.Models;
using RailworksForge.Core.Models.Examples;

namespace RailworksForge.ViewModels;

public class DesignReplaceConsistViewModel : ReplaceConsistViewModel
{
    public DesignReplaceConsistViewModel()
    {
        SaveConsists = Example.Scenario.Consists.ConvertAll(c => new SavedConsist
        {
            Consist = c,
            Name = $"[{c.BlueprintSetIdProvider}-{c.BlueprintSetIdProduct}] {c.LocomotiveName}",
        });

        SelectedConsist = SaveConsists.First();
    }
}
