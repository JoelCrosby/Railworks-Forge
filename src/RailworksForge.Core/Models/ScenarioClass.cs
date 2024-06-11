namespace RailworksForge.Core.Models;

public enum ScenarioClass
{
    Standard = 0,
    FreeRoam = 1,
    Career = 2,
    Template = 3,
    Timetable = 4,
    Unknown = 99,
}


public class ScenarioClassTypes
{
    public const string Standard = "eStandardScenarioClass";
    public const string FreeRoam = "eFreeRoamScenarioClass";
    public const string Career = "eCareerScenarioClass";
    public const string Template = "eTemplateScenarioClass";
    public const string Timetable = "eTimetableScenarioClass";

    public static ScenarioClass Parse(string name)
    {
        return name switch
        {
            Standard => ScenarioClass.Standard,
            FreeRoam => ScenarioClass.FreeRoam,
            Career => ScenarioClass.Career,
            Template => ScenarioClass.Template,
            Timetable => ScenarioClass.Timetable,
            _ => ScenarioClass.Unknown,
        };
    }
}
