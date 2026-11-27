using AngleSharp.Dom;

using RailworksForge.Core.Common;
using RailworksForge.Core.Extensions;

namespace RailworksForge.Core.Routes;

public sealed record RouteDetails
{
    public required LocalisedText Name { get; init; }

    public string AuthoredLanguage { get; init; } = string.Empty;

    // Set on DLC; the game's own editor refuses to change a locked route.
    public bool IsLocked { get; init; }

    public bool IsWorkshopItem { get; init; }

    public static RouteDetails Read(IDocument document)
    {
        var root = document.DocumentElement;

        return new RouteDetails
        {
            Name = root.ReadLocalisedText("DisplayName"),
            AuthoredLanguage = root.ReadValue("AuthoredLanguage"),
            IsLocked = root.ReadValue("Locked") is "1",
            IsWorkshopItem = root.ReadUnsignedLong("WorkshopId") is not 0,
        };
    }

    public void Write(IDocument document)
    {
        document.DocumentElement.WriteLocalisedText("DisplayName", Name, "ID");
    }
}
