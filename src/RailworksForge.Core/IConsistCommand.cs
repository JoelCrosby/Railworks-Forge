using AngleSharp.Dom;

namespace RailworksForge.Core;

public interface IConsistCommand
{
    Task Run(ConsistCommandContext context);
}
