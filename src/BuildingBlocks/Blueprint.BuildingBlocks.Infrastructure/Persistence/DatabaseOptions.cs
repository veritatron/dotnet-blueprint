namespace Blueprint.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Datenbank-Konfiguration (Abschnitt <c>Database</c>). Der Provider ist austauschbar (Oracle ↔ PostgreSQL),
/// ohne dass sich Domain- oder Application-Code ändert.
/// </summary>
/// <remarks>
/// Options-Pattern: https://learn.microsoft.com/dotnet/core/extensions/options<br/>
/// Mehrere Provider mit EF Core: https://learn.microsoft.com/ef/core/managing-schemas/migrations/providers
/// </remarks>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public DatabaseProvider Provider { get; set; } = DatabaseProvider.Oracle;

    /// <summary>
    /// Name des gemeinsamen Connection Strings. Ein Modul kann über einen eigenen Connection String
    /// (<c>ConnectionStrings:&lt;modulname&gt;</c>) auf eine separate Datenbank umgestellt werden –
    /// der erste Schritt bei der Auslagerung in einen Microservice ("Database per Service").
    /// </summary>
    public string ConnectionStringName { get; set; } = "blueprintdb";

    /// <summary>Oracle-SQL-Kompatibilität (z. B. <c>DatabaseVersion19</c>, <c>DatabaseVersion23</c>).</summary>
    public string OracleSqlCompatibility { get; set; } = "DatabaseVersion23";
}

public enum DatabaseProvider
{
    Oracle,
    PostgreSql,
}
