using RailworksForge.Core.Extensions;

namespace RailworksForge.Core.Consists.Commands;

public class FlipConsistVehicles : IConsistCommand
{
    private readonly FlipConsistVehiclesRequest _request;

    public FlipConsistVehicles(FlipConsistVehiclesRequest request)
    {
        _request = request;
    }

    public Task Run(ConsistCommandContext context)
    {
        var serviceConsist = ConsistVehicleElements.FindServiceConsist(context.ScenarioDocument, _request.Consist);

        foreach (var vehicle in _request.Vehicles)
        {
            var railVehicle = ConsistVehicleElements.FindVehicle(serviceConsist, vehicle);
            var flipped = railVehicle.QuerySelector("Flipped");

            if (flipped is null)
            {
                throw new Exception("rail vehicle has no flipped flag");
            }

            var isFlipped = flipped.TextContent == "1";
            var value = isFlipped ? "0" : "1";

            flipped.SetTextContent(value);
        }

        return Task.CompletedTask;
    }
}
