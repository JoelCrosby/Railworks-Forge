namespace RailworksForge.Core.Models;

public enum ScenarioClass
{
    Unknown = 0,
    Standard = 1,
    FreeRoam = 2,
    Career = 3,
    Template = 4,
    Timetable = 5,
}


public class ScenarioClassTypes
{
    private const string Standard = "eStandardScenarioClass";
    private const string FreeRoam = "eFreeRoamScenarioClass";
    private const string Career = "eCareerScenarioClass";
    private const string Template = "eTemplateScenarioClass";
    private const string Timetable = "eTimetableScenarioClass";

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
