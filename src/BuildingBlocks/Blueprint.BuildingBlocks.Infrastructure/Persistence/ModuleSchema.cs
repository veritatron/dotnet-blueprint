namespace Blueprint.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Liefert den providerspezifischen Schemanamen eines Moduls.
/// Oracle: Schema = Benutzer, Konvention GROSSBUCHSTABEN. PostgreSQL: Konvention kleinbuchstaben.
/// </summary>
/// <remarks>
/// Oracle Schemas: https://docs.oracle.com/en/database/oracle/oracle-database/23/cncpt/tables-and-table-clusters.html#GUID-2CEEA73E-8F0D-4A4A-A7AF-4C1E7B3C0BEB<br/>
/// EF Core Schemas: https://learn.microsoft.com/ef/core/modeling/entity-types#table-schema
/// </remarks>
public static class ModuleSchema
{
    public static string For(string moduleName, string? providerName) =>
        IsOracle(providerName) ? moduleName.ToUpperInvariant() : moduleName.ToLowerInvariant();

    public static bool IsOracle(string? providerName) =>
        providerName is not null && providerName.Contains("Oracle", StringComparison.OrdinalIgnoreCase);
}
