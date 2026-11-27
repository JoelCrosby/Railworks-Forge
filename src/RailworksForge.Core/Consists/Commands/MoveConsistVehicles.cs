using AngleSharp.Dom;

namespace RailworksForge.Core.Consists.Commands;

// Each step moves every selected vehicle one place past its unselected neighbour. A vehicle already at the end, or
// behind another selected vehicle that can't move, stays put, so a block of vehicles moves together.
public class MoveConsistVehicles : IConsistCommand
{
    private readonly MoveConsistVehiclesRequest _request;

    public MoveConsistVehicles(MoveConsistVehiclesRequest request)
    {
        _request = request;
    }

    public async Task Run(ConsistCommandContext context)
    {
        var serviceConsist = ConsistVehicleElements.FindServiceConsist(context.ScenarioDocument, _request.Consist);
        var selected = _request.Vehicles
            .Select(vehicle => ConsistVehicleElements.FindVehicle(serviceConsist, vehicle))
            .ToHashSet();

        if (selected.Count is 0)
        {
            return;
        }

        var original = ConsistVehicleOrder.GetVehicles(selected.First().ParentElement!);
        var ordered = new List<IElement>(original);
        var direction = Math.Sign(_request.Offset);
        var steps = Math.Abs(_request.Offset);

        for (var step = 0; step < steps; step++)
        {
            ShiftOnce(ordered, selected, direction);
        }

        var isUnchanged = ordered.SequenceEqual(original);

        if (isUnchanged)
        {
            return;
        }

        await ConsistVehicleOrder.Apply(context, _request.Consist, serviceConsist, ordered);
    }

    private static void ShiftOnce(List<IElement> vehicles, HashSet<IElement> selected, int direction)
    {
        var towardFront = direction < 0;
        var indexes = Enumerable.Range(0, vehicles.Count).ToList();

        if (!towardFront)
        {
            indexes.Reverse();
        }

        foreach (var index in indexes)
        {
            var target = index + direction;
            var isInRange = target >= 0 && target < vehicles.Count;

            if (!isInRange)
            {
                continue;
            }

            var canSwap = selected.Contains(vehicles[index]) && !selected.Contains(vehicles[target]);

            if (canSwap)
            {
                (vehicles[index], vehicles[target]) = (vehicles[target], vehicles[index]);
            }
        }
    }
}
