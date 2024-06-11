namespace RailworksForge.Models;

public enum BreadcrumbLink
{
    Routes = 0,
    Route = 1,
    Scenario = 2,
}

public record Breadcrumb(string Name, BreadcrumbLink Link);
