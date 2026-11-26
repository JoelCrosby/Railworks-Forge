namespace RailworksForge.Core.Consists.Commands;

public interface IConsistCommand
{
    Task Run(ConsistCommandContext context);
}
