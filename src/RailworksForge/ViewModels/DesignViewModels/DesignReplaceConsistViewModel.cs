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
            Name = $"[{c.BlueprintSetIdProvider}-{c.BlueprintSetIdProduct}] {c.LocomotiveName}",
            LocomotiveName = c.LocomotiveName,
            ConsistElement = string.Empty,
        });

        SelectedConsist = SaveConsists.First();
    }
}
