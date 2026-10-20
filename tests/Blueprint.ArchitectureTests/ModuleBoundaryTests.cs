using System.Reflection;

namespace Blueprint.ArchitectureTests;

/// <summary>
/// Architekturregeln des modularen Monolithen – Grundlage für eine spätere, risikoarme Auslagerung in Microservices.
/// </summary>
/// <remarks>
/// Prüft die tatsächlich kompilierten Assembly-Referenzen (Reflection, ohne Fremdbibliothek).
/// Abhängigkeitsregeln der Clean Architecture:
/// https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures#clean-architecture
/// </remarks>
[TestClass]
public sealed class ModuleBoundaryTests
{
    private static readonly string[] Modules = ["Catalog", "Ordering"];

    private static readonly string[] Layers = ["Domain", "Application", "Infrastructure", "Contracts", "Client"];

    public static IEnumerable<object[]> ModuleAssemblies =>
        from module in Modules
        from layer in Layers
        let assembly = TryLoad($"Blueprint.Modules.{module}.{layer}")
        where assembly is not null
        select new object[] { module, layer };

    [TestMethod]
    [DynamicData(nameof(ModuleAssemblies))]
    public void Modules_OnlyReferenceContractsOfOtherModules(string module, string layer)
    {
        var references = ReferencedBlueprintAssemblies($"Blueprint.Modules.{module}.{layer}");

        var violations = references
            .Where(name => name.StartsWith("Blueprint.Modules.", StringComparison.Ordinal))
            .Where(name => !name.StartsWith($"Blueprint.Modules.{module}.", StringComparison.Ordinal))
            .Where(name => !name.EndsWith(".Contracts", StringComparison.Ordinal))
            .ToList();

        Assert.IsEmpty(violations, $"{module}.{layer} referenziert Interna anderer Module: {string.Join(", ", violations)}");
    }

    [TestMethod]
    [DataRow("Catalog")]
    [DataRow("Ordering")]
    public void Domain_HasNoInfrastructureDependencies(string module)
    {
        var references = Load($"Blueprint.Modules.{module}.Domain").GetReferencedAssemblies().Select(a => a.Name!).ToList();

        string[] forbidden = ["Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Oracle", "Npgsql", "Blueprint.BuildingBlocks.Infrastructure"];
        var violations = references.Where(r => forbidden.Any(f => r.StartsWith(f, StringComparison.Ordinal))).ToList();

        Assert.IsEmpty(violations, $"{module}.Domain muss persistenz- und framework-unabhängig bleiben: {string.Join(", ", violations)}");
    }

    [TestMethod]
    [DataRow("Catalog")]
    [DataRow("Ordering")]
    public void Application_DoesNotReferenceInfrastructure(string module)
    {
        var references = Load($"Blueprint.Modules.{module}.Application").GetReferencedAssemblies().Select(a => a.Name!).ToList();

        string[] forbidden = ["Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Blueprint.BuildingBlocks.Infrastructure"];
        var violations = references.Where(r => forbidden.Any(f => r.StartsWith(f, StringComparison.Ordinal))).ToList();

        Assert.IsEmpty(violations, $"{module}.Application darf keine Infrastruktur referenzieren: {string.Join(", ", violations)}");
    }

    [TestMethod]
    [DataRow("Catalog")]
    [DataRow("Ordering")]
    public void Contracts_OnlyDependOnBuildingBlocksContracts(string module)
    {
        var violations = ReferencedBlueprintAssemblies($"Blueprint.Modules.{module}.Contracts")
            .Where(name => name != "Blueprint.BuildingBlocks.Contracts")
            .ToList();

        Assert.IsEmpty(violations, $"{module}.Contracts muss eigenständig paketierbar bleiben: {string.Join(", ", violations)}");
    }

    [TestMethod]
    public void BlazorClient_OnlyReferencesGeneratedApiClients()
    {
        var violations = ReferencedBlueprintAssemblies("Blueprint.Web.Client")
            .Where(name => !name.EndsWith(".Client", StringComparison.Ordinal))
            .ToList();

        Assert.IsEmpty(violations, $"Der WebAssembly-Client darf nur die generierten API-Clients kennen: {string.Join(", ", violations)}");
    }

    [TestMethod]
    [DataRow("Catalog")]
    [DataRow("Ordering")]
    public void GeneratedClients_DoNotReferenceModuleInternals(string module)
    {
        var violations = ReferencedBlueprintAssemblies($"Blueprint.Modules.{module}.Client")
            .Where(name => !name.EndsWith(".Contracts", StringComparison.Ordinal))
            .ToList();

        Assert.IsEmpty(violations, $"{module}.Client darf nur Contracts referenzieren (unabhängig deploybar): {string.Join(", ", violations)}");
    }

    private static List<string> ReferencedBlueprintAssemblies(string assemblyName) =>
        Load(assemblyName).GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .Where(name => name.StartsWith("Blueprint.", StringComparison.Ordinal))
            .ToList();

    private static Assembly Load(string assemblyName) => Assembly.Load(assemblyName);

    private static Assembly? TryLoad(string assemblyName)
    {
        try
        {
            return Assembly.Load(assemblyName);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }
}
