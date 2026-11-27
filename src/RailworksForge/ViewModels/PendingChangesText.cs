using System.Collections.Generic;
using System.Linq;

using RailworksForge.Core.Scenarios;
using RailworksForge.Translations;

namespace RailworksForge.ViewModels;

public static class PendingChangesText
{
    private const int MaxDescribedChanges = 3;

    public static string Summary(IReadOnlyList<ScenarioChange> changes)
    {
        var isSingleChange = changes.Count is 1;

        return isSingleChange
            ? Strings.unsaved_change_one.CurrentValue
            : string.Format(Strings.unsaved_changes_many.CurrentValue, changes.Count);
    }

    public static string Details(IReadOnlyList<ScenarioChange> changes)
    {
        var recentChanges = changes.TakeLast(MaxDescribedChanges).Select(Describe);
        var described = string.Join(" · ", recentChanges);

        return $"{described}. {Strings.backup_before_saving.CurrentValue}";
    }

    public static string Describe(ScenarioChange change)
    {
        var isSingleService = change.Count is 1 && change.ServiceName.Length > 0;

        return change.Kind switch
        {
            ScenarioChangeKind.ReplaceConsists when isSingleService => Format(Strings.change_replaced_consist.CurrentValue, change.ServiceName),
            ScenarioChangeKind.ReplaceConsists => Format(Strings.change_replaced_consists.CurrentValue, change.Count),
            ScenarioChangeKind.DeleteConsists when isSingleService => Format(Strings.change_deleted_consist.CurrentValue, change.ServiceName),
            ScenarioChangeKind.DeleteConsists => Format(Strings.change_deleted_consists.CurrentValue, change.Count),
            ScenarioChangeKind.AddVehicle => Format(Strings.change_added_vehicle.CurrentValue, change.ServiceName),
            ScenarioChangeKind.ReplaceVehicles => string.Format(Strings.change_replaced_vehicles.CurrentValue, change.Count, change.ServiceName),
            ScenarioChangeKind.DeleteVehicle => Format(Strings.change_deleted_vehicle.CurrentValue, change.ServiceName),
            ScenarioChangeKind.MoveVehicle => Format(Strings.change_moved_vehicle.CurrentValue, change.ServiceName),
            ScenarioChangeKind.FlipVehicle => Format(Strings.change_flipped_vehicle.CurrentValue, change.ServiceName),
            _ => string.Empty,
        };
    }

    private static string Format(string format, object value)
    {
        return string.Format(format, value);
    }
}
