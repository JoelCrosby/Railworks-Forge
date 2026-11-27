using RailworksForge.Core.Extensions;

namespace RailworksForge.Core.Consists.Commands;

public class FlipConsistVehicle : IConsistCommand
{
    private readonly FlipConsistVehicleRequest _request;

    public FlipConsistVehicle(FlipConsistVehicleRequest request)
    {
        _request = request;
    }

    public Task Run(ConsistCommandContext context)
    {
        var serviceConsist = ConsistVehicleElements.FindServiceConsist(context.ScenarioDocument, _request.Consist);
        var railVehicle = ConsistVehicleElements.FindVehicle(serviceConsist, _request.Vehicle);
        var flipped = railVehicle.QuerySelector("Flipped");

        if (flipped is null)
        {
            throw new Exception("rail vehicle has no flipped flag");
        }

        var isFlipped = flipped.TextContent == "1";
        var value = isFlipped ? "0" : "1";

        flipped.SetTextContent(value);

        return Task.CompletedTask;
    }
}
