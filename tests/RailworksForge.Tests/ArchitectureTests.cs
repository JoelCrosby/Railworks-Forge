using Mono.Cecil;
using Mono.Cecil.Cil;

using RailworksForge.Core.Common;

namespace RailworksForge.Tests;

// The UI presents data and forwards user intent; finding, reading and writing game files belongs to Core services.
// Reads the UI assembly's IL, so calls inside lambdas, async methods and compiled bindings are covered too.
public class ArchitectureTests
{
    private static readonly string[] ForbiddenTypes =
    [
        "RailworksForge.Core.Assets.Archives",
        "RailworksForge.Core.Common.Cache",
        "RailworksForge.Core.Common.Paths",
        "RailworksForge.Core.Common.Utilities",
        "RailworksForge.Core.Common.XmlParser",
        "RailworksForge.Core.Config.Configuration",
        "RailworksForge.Core.Packaging.Packager",
        "RailworksForge.Core.Scenarios.ScenarioWriter",
        "RailworksForge.Core.Serialization.Serz",
        "RailworksForge.Core.Serialization.SerzInternal",
        "RailworksForge.Core.Serialization.SerzReader",
        "System.IO.Compression.ZipArchive",
        "System.IO.Compression.ZipFile",
        "System.IO.Directory",
        "System.IO.DirectoryInfo",
        "System.IO.File",
        "System.IO.FileInfo",
    ];

    private static readonly string[] ForbiddenNamespaces =
    [
        "AngleSharp",
    ];

    private static readonly string[] ForbiddenMembers =
    [
        "RailworksForge.Core.Assets.Blueprint::GetXmlDocument",
        "RailworksForge.Core.Routes.Route::CreateBackup",
        "RailworksForge.Core.Routes.Route::ExtractScenarios",
        "RailworksForge.Core.Routes.Route::GetRoutePropertiesDocument",
        "RailworksForge.Core.Routes.Route::GetTrackBlueprints",
        "RailworksForge.Core.Routes.Route::GetTrackDocument",
        "RailworksForge.Core.Scenarios.Scenario::ConvertBinToXml",
        "RailworksForge.Core.Scenarios.Scenario::ConvertXmlToBin",
        "RailworksForge.Core.Scenarios.Scenario::CreateBackup",
        "RailworksForge.Core.Scenarios.Scenario::ExportBinToXml",
        "RailworksForge.Core.Scenarios.Scenario::GetXmlDocument",
        "RailworksForge.Core.Scenarios.ScenarioEditSession::Apply",
        "RailworksForge.Core.Scenarios.ScenarioEditSession::Begin",
    ];

    // The launcher hands folders to the OS shell, which needs them to exist first.
    private static readonly Dictionary<string, string[]> Exemptions = new()
    {
        ["RailworksForge.Services.LauncherService"] = ["System.IO.Directory", "System.IO.DirectoryInfo"],
    };

    [Fact]
    public void Ui_DoesNotReachIntoCoreInternals()
    {
        using var module = ModuleDefinition.ReadModule(typeof(App).Assembly.Location);

        var violations = module.GetTypes()
            .Where(type => !IsCoverageInstrumentation(type))
            .SelectMany(type => type.Methods)
            .Where(method => method.HasBody)
            .SelectMany(GetViolations)
            .Distinct()
            .Order()
            .ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void ForbiddenCoreReferences_StillExist()
    {
        var coreAssembly = typeof(Paths).Assembly;
        var coreTypes = ForbiddenTypes.Where(name => name.StartsWith("RailworksForge.Core.")).ToList();
        var missingTypes = coreTypes.Where(name => coreAssembly.GetType(name) is null).ToList();
        var missingMembers = ForbiddenMembers.Where(member => !CoreMemberExists(coreAssembly, member)).ToList();
        var missingReferences = missingTypes.Concat(missingMembers).ToList();

        Assert.Empty(missingReferences);
    }

    private static IEnumerable<string> GetViolations(MethodDefinition method)
    {
        var owner = GetOutermostType(method.DeclaringType).FullName;
        var references = method.Body.Instructions.Select(GetReferencedMember).OfType<MemberReference>();

        foreach (var reference in references)
        {
            var referencedType = GetReferencedType(reference);
            var referencedMember = $"{referencedType}::{reference.Name}";

            var isForbiddenType = ForbiddenTypes.Contains(referencedType) && !IsExempt(owner, referencedType);
            var isForbiddenNamespace = ForbiddenNamespaces.Any(ns => referencedType.StartsWith($"{ns}."));
            var isForbiddenMember = ForbiddenMembers.Contains(referencedMember);
            var isViolation = isForbiddenType || isForbiddenNamespace || isForbiddenMember;

            if (isViolation)
            {
                yield return $"{owner} uses {referencedMember}";
            }
        }
    }

    private static MemberReference? GetReferencedMember(Instruction instruction)
    {
        return instruction.Operand as MemberReference;
    }

    private static string GetReferencedType(MemberReference reference)
    {
        var type = reference as TypeReference ?? reference.DeclaringType;

        return type.GetElementType().FullName;
    }

    private static TypeDefinition GetOutermostType(TypeDefinition type)
    {
        var outermost = type;

        while (outermost.DeclaringType is not null)
        {
            outermost = outermost.DeclaringType;
        }

        return outermost;
    }

    // Coverage runs inject a hit tracker into the UI assembly, and it writes its results with System.IO.File.
    private static bool IsCoverageInstrumentation(TypeDefinition type)
    {
        return type.Namespace.StartsWith("Coverlet.");
    }

    private static bool IsExempt(string owner, string referencedType)
    {
        var exemptTypes = Exemptions.GetValueOrDefault(owner) ?? [];

        return exemptTypes.Contains(referencedType);
    }

    private static bool CoreMemberExists(System.Reflection.Assembly coreAssembly, string member)
    {
        var parts = member.Split("::");
        var type = coreAssembly.GetType(parts[0]);
        var members = type?.GetMember(parts[1]) ?? [];

        return members.Length > 0;
    }
}
