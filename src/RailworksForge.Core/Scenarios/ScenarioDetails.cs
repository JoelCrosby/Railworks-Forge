using AngleSharp.Dom;

using RailworksForge.Core.Common;
using RailworksForge.Core.Extensions;

namespace RailworksForge.Core.Scenarios;

public enum ScenarioSeason
{
    None,
    Spring,
    Summer,
    Autumn,
    Winter,
}

// The parts of ScenarioProperties.xml that describe a scenario rather than how it runs.
public sealed record ScenarioDetails
{
    public required LocalisedText Name { get; init; }

    public required LocalisedText Description { get; init; }

    public required LocalisedText Briefing { get; init; }

    public required LocalisedText StartLocation { get; init; }

    public int DurationMinutes { get; init; }

    public int Rating { get; init; }

    public ScenarioSeason Season { get; init; }

    public DateOnly? StartDate { get; init; }

    public TimeSpan StartTime { get; init; }

    public ScenarioClass ScenarioClass { get; init; }

    public bool IsWorkshopItem { get; init; }

    public static ScenarioDetails Read(IDocument document)
    {
        var root = document.DocumentElement;

        return new ScenarioDetails
        {
            Name = root.ReadLocalisedText("DisplayName"),
            Description = root.ReadLocalisedText("Description"),
            Briefing = root.ReadLocalisedText("Briefing"),
            StartLocation = root.ReadLocalisedText("StartLocation"),
            DurationMinutes = root.ReadInteger("DurationMins"),
            Rating = root.ReadInteger("Rating"),
            Season = ParseSeason(root.ReadValue("Season")),
            StartDate = ReadStartDate(root),
            StartTime = ReadStartTime(root),
            ScenarioClass = ScenarioClassTypes.Parse(root.ReadValue("ScenarioClass")),
            IsWorkshopItem = root.ReadUnsignedLong("WorkshopId") is not 0,
        };
    }

    // Only fields that differ from what was read are written, so values the editor can't represent, such as a start
    // time past midnight, survive an edit to something else.
    public void Write(IDocument document, ScenarioDetails original)
    {
        var root = document.DocumentElement;

        WriteTextIfChanged(root, "DisplayName", Name, original.Name, "ID");
        WriteTextIfChanged(root, "Description", Description, original.Description, "DisplayName");
        WriteTextIfChanged(root, "Briefing", Briefing, original.Briefing, "Description");
        WriteTextIfChanged(root, "StartLocation", StartLocation, original.StartLocation, "Briefing");

        if (DurationMinutes != original.DurationMinutes)
        {
            root.WriteInteger("DurationMins", DurationMinutes);
        }

        if (Rating != original.Rating)
        {
            root.WriteInteger("Rating", Rating);
        }

        if (StartTime != original.StartTime)
        {
            root.WriteFloat("StartTime", StartTime.TotalSeconds);
        }

        var hasNewSeason = Season != original.Season && Season is not ScenarioSeason.None;

        if (hasNewSeason)
        {
            root.WriteValue("Season", "cDeltaString", $"SEASON_{Season.ToString().ToUpperInvariant()}");
        }

        var hasNewStartDate = StartDate != original.StartDate && StartDate is not null;

        if (hasNewStartDate)
        {
            var startDate = StartDate!.Value;

            root.WriteInteger("StartDD", startDate.Day);
            root.WriteInteger("StartMM", startDate.Month);
            root.WriteInteger("StartYYYY", startDate.Year);
        }
    }

    private static void WriteTextIfChanged(
        IElement root,
        string name,
        LocalisedText text,
        LocalisedText original,
        string precedingName)
    {
        if (text != original)
        {
            root.WriteLocalisedText(name, text, precedingName);
        }
    }

    // Shows the edits on a scenario before they are saved, the way Scenario.New would read them back.
    public Scenario ApplyTo(Scenario scenario)
    {
        var name = Name.DisplayText;
        var season = Season is ScenarioSeason.None ? string.Empty : Season.ToString();

        return scenario with
        {
            Name = name,
            SearchIndex = name.ToLowerInvariant(),
            Description = Description.DisplayText,
            Briefing = Briefing.DisplayText,
            StartLocation = StartLocation.DisplayText,
            Duration = DurationMinutes,
            Rating = Rating,
            Season = season,
        };
    }

    private static ScenarioSeason ParseSeason(string code)
    {
        return code switch
        {
            "SEASON_SPRING" => ScenarioSeason.Spring,
            "SEASON_SUMMER" => ScenarioSeason.Summer,
            "SEASON_AUTUMN" => ScenarioSeason.Autumn,
            "SEASON_WINTER" => ScenarioSeason.Winter,
            _ => ScenarioSeason.None,
        };
    }

    private static DateOnly? ReadStartDate(IElement root)
    {
        var day = root.ReadInteger("StartDD");
        var month = root.ReadInteger("StartMM");
        var year = root.ReadInteger("StartYYYY");
        var isValidMonth = month is >= 1 and <= 12;
        var isValidYear = year is >= 1 and <= 9999;
        var isValidDay = isValidMonth && isValidYear && day >= 1 && day <= DateTime.DaysInMonth(year, month);

        return isValidDay ? new DateOnly(year, month, day) : null;
    }

    private static TimeSpan ReadStartTime(IElement root)
    {
        var seconds = root.ReadDouble("StartTime");
        var isWithinDay = seconds is >= 0 and < 86400;

        return isWithinDay ? TimeSpan.FromSeconds(Math.Round(seconds)) : TimeSpan.Zero;
    }
}
