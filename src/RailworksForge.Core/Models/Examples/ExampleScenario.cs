using RailworksForge.Core.Types;

namespace RailworksForge.Core.Models.Examples;

public static partial class Example
{
    public static Scenario Scenario => new()
    {
        Id = "a8a66429-5f23-44e1-bb3c-2ba1f6bcc29b",
        Route = Route,
        Name = "MNWPH4-08: 1S96 1621 Willesden P.R.D.C. - Shieldmuir",
        Description = "Year: 2021. Drive 325005/325006 from Colwich to Crewe, via Stoke.",
        Briefing = "An early evening run from Colwich to Crewe with an 8-car Class 325 consist, forming a Willesden to Shieldmuir mail working, diverted today via Stoke due to operational issues. Be aware also of a 60MPH TSR at Hixon.",
        StartLocation = "Colwich",
        Locomotive = "Class 390 'Pendolino'",
        DirectoryPath = "/cache/SteamLibrary/steamapps/common/RailWorks/Content/Routes/045911ae-114c-4dfc-8382-4505d0491555/Scenarios/a8a66429-5f23-44e1-bb3c-2ba1f6bcc29b",
        PackagingType = PackagingType.Unpacked,
        ScenarioPropertiesPath = "/cache/SteamLibrary/steamapps/common/RailWorks/Content/Routes/045911ae-114c-4dfc-8382-4505d0491555/Scenarios/a8a66429-5f23-44e1-bb3c-2ba1f6bcc29b/ScenarioProperties.xml",
        ScenarioClass = ScenarioClass.Standard,
        AssetPath = AssetPath.Empty,
        Duration = 60,
        Rating = 3,
        Consists =
        [
            new()
            {
                LocomotiveName = "Class 390 'Pendolino'",
                Id = "520411720",
                ServiceName = "9M59 1747 Blackpool North - London Euston",
                BlueprintId = "RailVehicles\\Electric\\Class390\\Default\\DMRF\\Class390DMRF.xml",
                ServiceId = "1f792a70-7095-414d-8a1a-bb9016411cf6",
                LocoAuthor = "DTG",
                LocoClass = LocoClass.Electric,
                BlueprintSetIdProduct = "WCML-South",
                BlueprintSetIdProvider = "DTG",
            },
            new()
            {
                LocomotiveName = "Class 150/2 Ex-Arriva Trains Wales",
                Id = "520411788",
                ServiceName = "1V62 1831 Manchester Piccadilly - Camarthen",
                BlueprintId = "RailVehicles\\Class150\\Ex-ATW2_AP\\Class150_DMS.xml",
                ServiceId = "d4b19b94-6cd4-4589-b7d2-38720d301ec1",
                LocoAuthor = "AP",
                LocoClass = LocoClass.Diesel,
                BlueprintSetIdProduct = "Class150Pack01",
                BlueprintSetIdProvider = "AP",
            },
            new()
            {
                LocomotiveName = "Class 390 'Pendolino'",
                Id = "520410156",
                ServiceName = "1M15 1434 Glasgow Central - London Euston",
                BlueprintId = "RailVehicles\\Diesel\\221\\Brown\\Engine\\a25_locomotive_221_dmf_xc.xml",
                ServiceId = Guid.NewGuid().ToString(),
                LocoAuthor = "DTG",
                LocoClass = LocoClass.Electric,
                BlueprintSetIdProduct = "WCML-South",
                BlueprintSetIdProvider = "DTG",
            },
            new()
            {
                LocomotiveName = "Class 66 GB Railfreight",
                Id = "520409612",
                ServiceName = "0Z70 1745 Longport F.D. - Willesden Euroterminal",
                BlueprintId = "RailVehicles\\Diesel\\221\\Brown\\Engine\\a25_locomotive_221_dmf_xc.xml",
                ServiceId = Guid.NewGuid().ToString(),
                LocoAuthor = "RSC",
                LocoClass = LocoClass.Diesel,
                BlueprintSetIdProduct = "Class66Pack03",
                BlueprintSetIdProvider = "RSC",
            },
            new()
            {
                LocomotiveName = "JT Class 220 CC A (DMF)",
                Id = "520411856",
                ServiceName = "1M58 1616 Reading - Manchester Piccadilly",
                BlueprintId = "RailVehicles\\Diesel\\221\\Brown\\Engine\\a25_locomotive_221_dmf_xc.xml",
                ServiceId = Guid.NewGuid().ToString(),
                LocoAuthor = "JustTrains",
                LocoClass = LocoClass.Diesel,
                BlueprintSetIdProduct = "Voyager",
                BlueprintSetIdProvider = "JustTrains",
            },
            new()
            {
                LocomotiveName = "Class 350 London North Western Railway",
                Id = "520413352",
                ServiceName = "2G76 1801 Crewe - Birmingham New Street",
                BlueprintId = "RailVehicles\\Diesel\\221\\Brown\\Engine\\a25_locomotive_221_dmf_xc.xml",
                ServiceId = Guid.NewGuid().ToString(),
                LocoAuthor = "DTG",
                LocoClass = LocoClass.Electric,
                BlueprintSetIdProduct = "PortsmouthDirect",
                BlueprintSetIdProvider = "DTG",
            },
            new()
            {
                LocomotiveName = "Class 325 Royal Mail Revised",
                Id = "520412332",
                ServiceName = "1S96",
                BlueprintId = "RailVehicles\\Diesel\\221\\Brown\\Engine\\a25_locomotive_221_dmf_xc.xml",
                ServiceId = Guid.NewGuid().ToString(),
                LocoAuthor = "RSC",
                LocoClass = LocoClass.Electric,
                BlueprintSetIdProduct = "Class325Pack01",
                BlueprintSetIdProvider = "RSC",
            },
            new()
            {
                LocomotiveName = "Class 66 Colas Rail Freight",
                Id = "520410428",
                ServiceName = "6K38 1803 Pinnox - Crewe Basford Hall",
                BlueprintId = "RailVehicles\\Diesel\\221\\Brown\\Engine\\a25_locomotive_221_dmf_xc.xml",
                ServiceId = Guid.NewGuid().ToString(),
                LocoAuthor = "RSC",
                LocoClass = LocoClass.Diesel,
                BlueprintSetIdProduct = string.Empty,
                BlueprintSetIdProvider = string.Empty,
            },
            new()
            {
                LocomotiveName = "Class 390 'Pendolino'",
                Id = "520411992",
                ServiceName = "1A62 1815 Manchester Piccadilly - London Euston",
                BlueprintId = "RailVehicles\\Diesel\\221\\Brown\\Engine\\a25_locomotive_221_dmf_xc.xml",
                ServiceId = Guid.NewGuid().ToString(),
                LocoAuthor = "DTG",
                LocoClass = LocoClass.Electric,
                BlueprintSetIdProduct = string.Empty,
                BlueprintSetIdProvider = string.Empty,
            },
            new()
            {
                LocomotiveName = "Class 319 Northern",
                Id = "520409952",
                ServiceName = "5K45 1837 Stoke-on-Trent - Crewe",
                BlueprintId = "RailVehicles\\Diesel\\221\\Brown\\Engine\\a25_locomotive_221_dmf_xc.xml",
                ServiceId = Guid.NewGuid().ToString(),
                LocoAuthor = "AP",
                LocoClass = LocoClass.Electric,
                BlueprintSetIdProduct = string.Empty,
                BlueprintSetIdProvider = string.Empty,
            },
            new()
            {
                LocomotiveName = "Class 150/2 Ex-Arriva Trains Wales",
                Id = "520413420",
                ServiceName = "1D63 1934 Crewe - Chester",
                BlueprintId = "RailVehicles\\Diesel\\221\\Brown\\Engine\\a25_locomotive_221_dmf_xc.xml",
                ServiceId = Guid.NewGuid().ToString(),
                LocoAuthor = "AP",
                LocoClass = LocoClass.Diesel,
                BlueprintSetIdProduct = string.Empty,
                BlueprintSetIdProvider = string.Empty,
            },
            new()
            {
                LocomotiveName = "Class 319 Northern",
                Id = "520411924",
                ServiceName = "2F11 1917 Crewe - Liverpool Lime Street",
                BlueprintId = "RailVehicles\\Diesel\\221\\Brown\\Engine\\a25_locomotive_221_dmf_xc.xml",
                ServiceId = Guid.NewGuid().ToString(),
                LocoAuthor = "AP",
                LocoClass = LocoClass.Electric,
                BlueprintSetIdProduct = string.Empty,
                BlueprintSetIdProvider = string.Empty,
            },
            new()
            {
                LocomotiveName = "Class 350 London North Western Railway",
                Id = "520409680",
                ServiceName = "1U43 1646 London Euston - Crewe",
                BlueprintId = "RailVehicles\\Diesel\\221\\Brown\\Engine\\a25_locomotive_221_dmf_xc.xml",
                ServiceId = Guid.NewGuid().ToString(),
                LocoAuthor = "DTG",
                LocoClass = LocoClass.Electric,
                BlueprintSetIdProduct = string.Empty,
                BlueprintSetIdProvider = string.Empty,
            },
            new()
            {
                LocomotiveName = "Class 319 Northern",
                Id = "520412060",
                ServiceName = "2H82 1858 Stoke-on-Trent - Manchester Piccadilly",
                BlueprintId = "RailVehicles\\Diesel\\221\\Brown\\Engine\\a25_locomotive_221_dmf_xc.xml",
                ServiceId = Guid.NewGuid().ToString(),
                LocoAuthor = "AP",
                LocoClass = LocoClass.Electric,
                BlueprintSetIdProduct = string.Empty,
                BlueprintSetIdProvider = string.Empty,
            },
            new()
            {
                LocomotiveName = "Class 156 Ex-East Midlands Trains (East Midlands Railway)",
                Id = "520411040",
                ServiceName = "2A73 1909 Crewe - Nottingham",
                BlueprintId = "RailVehicles\\Diesel\\221\\Brown\\Engine\\a25_locomotive_221_dmf_xc.xml",
                ServiceId = Guid.NewGuid().ToString(),
                LocoAuthor = "AP",
                LocoClass = LocoClass.Diesel,
                BlueprintSetIdProduct = string.Empty,
                BlueprintSetIdProvider = string.Empty,
            },
            new()
            {
                LocomotiveName = "Class 350 London North Western Railway",
                Id = "520413692",
                ServiceName = "2G80 1901 Crewe - Birmingham New Street",
                BlueprintId = "RailVehicles\\Diesel\\221\\Brown\\Engine\\a25_locomotive_221_dmf_xc.xml",
                ServiceId = Guid.NewGuid().ToString(),
                LocoAuthor = "DTG",
                LocoClass = LocoClass.Electric,
                BlueprintSetIdProduct = string.Empty,
                BlueprintSetIdProvider = string.Empty,
            },
            new()
            {
                LocomotiveName = "Class 66 Freightliner PowerHaul",
                Id = "520413488",
                ServiceName = "4M87 1113 Felixsrowe North F.L.T. - Trafford Park",
                BlueprintId = "RailVehicles\\Diesel\\221\\Brown\\Engine\\a25_locomotive_221_dmf_xc.xml",
                ServiceId = Guid.NewGuid().ToString(),
                LocoAuthor = "RSC",
                LocoClass = LocoClass.Diesel,
                BlueprintSetIdProduct = string.Empty,
                BlueprintSetIdProvider = string.Empty,
            },
        ],
    };
}
