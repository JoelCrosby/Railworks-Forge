using System;

using Avalonia.Data.Converters;

using LucideAvalonia.Enum;

using RailworksForge.Core.Models;
using RailworksForge.Translations;

namespace RailworksForge.Converters;

public static class ValueConverters
{
    private const int MaximumRating = 5;

    public static FuncValueConverter<string?, bool> IsCompleteConverter { get; } = new (value =>
    {
        return value switch
        {
            "CompletedSuccessfully" => true,
            _ => false,
        };
    });

    public static FuncValueConverter<int, string> DurationConverter { get; } = new (minutes =>
    {
        var hours = minutes / 60;
        var remainingMinutes = minutes % 60;

        return (hours, remainingMinutes) switch
        {
            (0, 0) => "–",
            (0, _) => $"{remainingMinutes}m",
            (_, 0) => $"{hours}h",
            _ => $"{hours}h {remainingMinutes:00}m",
        };
    });

    public static FuncValueConverter<int, string> RatingConverter { get; } = new (rating =>
    {
        var filled = Math.Clamp(rating, 0, MaximumRating);
        var empty = MaximumRating - filled;

        return new string('●', filled) + new string('○', empty);
    });

    public static FuncValueConverter<ScenarioClass, string> ScenarioClassConverter { get; } = new (value =>
    {
        var label = value switch
        {
            ScenarioClass.Standard => Strings.scenario_class_standard,
            ScenarioClass.FreeRoam => Strings.scenario_class_free_roam,
            ScenarioClass.Career => Strings.scenario_class_career,
            ScenarioClass.Template => Strings.scenario_class_template,
            ScenarioClass.Timetable => Strings.scenario_class_timetable,
            _ => Strings.scenario_class_unknown,
        };

        return label.CurrentValue;
    });

    public static FuncValueConverter<PackagingType, string> PackagingTypeConverter { get; } = new (value =>
    {
        var label = value is PackagingType.Packed ? Strings.packaging_packed : Strings.unpacked;

        return label.CurrentValue;
    });

    public static FuncValueConverter<AcquisitionState, string> AcquisitionStateConverter { get; } = new (value =>
    {
        var label = value switch
        {
            AcquisitionState.Found => Strings.acquisition_found,
            AcquisitionState.Partial => Strings.acquisition_partial,
            AcquisitionState.Missing => Strings.acquisition_missing,
            _ => Strings.acquisition_unknown,
        };

        return label.CurrentValue;
    });

    // Seasons arrive as the English names Core parses from the scenario file, and the season icons match on them.
    public static FuncValueConverter<string?, string> SeasonConverter { get; } = new (value =>
    {
        var label = value switch
        {
            "Spring" => Strings.season_spring,
            "Summer" => Strings.season_summer,
            "Autumn" => Strings.season_autumn,
            "Winter" => Strings.season_winter,
            _ => null,
        };

        return label?.CurrentValue ?? value ?? string.Empty;
    });

    // One icon per cell, switched by value: each Lucide control builds its own copy of the icon dictionary, so
    // stacking a hidden icon per possible value multiplied that cost for every row.
    public static FuncValueConverter<ScenarioClass, LucideIconNames> ScenarioClassIconConverter { get; } = new (value =>
    {
        return value switch
        {
            ScenarioClass.Standard => LucideIconNames.Flag,
            ScenarioClass.FreeRoam => LucideIconNames.MapPinned,
            ScenarioClass.Career => LucideIconNames.Trophy,
            ScenarioClass.Timetable => LucideIconNames.CalendarClock,
            ScenarioClass.Template => LucideIconNames.LayoutTemplate,
            _ => LucideIconNames.CircleQuestionMark,
        };
    });

    public static FuncValueConverter<string?, LucideIconNames> SeasonIconConverter { get; } = new (value =>
    {
        return value switch
        {
            "Spring" => LucideIconNames.Flower2,
            "Summer" => LucideIconNames.SunMedium,
            "Autumn" => LucideIconNames.Leaf,
            "Winter" => LucideIconNames.Snowflake,
            _ => LucideIconNames.CircleQuestionMark,
        };
    });

    public static FuncValueConverter<AcquisitionState, LucideIconNames> AcquisitionStateIconConverter { get; } = new (value =>
    {
        return value switch
        {
            AcquisitionState.Found => LucideIconNames.CircleCheck,
            AcquisitionState.Partial => LucideIconNames.TriangleAlert,
            AcquisitionState.Missing => LucideIconNames.CircleX,
            _ => LucideIconNames.CircleQuestionMark,
        };
    });

    public static FuncValueConverter<AcquisitionState, string> AcquisitionStateTooltipConverter { get; } = new (value =>
    {
        var label = value switch
        {
            AcquisitionState.Found => Strings.assets_found,
            AcquisitionState.Partial => Strings.assets_partial,
            AcquisitionState.Missing => Strings.assets_missing,
            _ => Strings.assets_unknown,
        };

        return label.CurrentValue;
    });
}
