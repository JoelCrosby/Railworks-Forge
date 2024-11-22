namespace RailworksForge.Core.Commands.Common;

public interface IConsistCommand
{
    Task Run(ConsistCommandContext context);
}
