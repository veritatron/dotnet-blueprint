using Blueprint.BuildingBlocks.Infrastructure.DomainEvents;
using Blueprint.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Blueprint.Modules.Ordering.Infrastructure.Persistence;

/// <summary>
/// Design-Time-Factories für <c>dotnet ef</c>: Migrations können ohne laufende Datenbank und ohne App-Start erzeugt werden.
/// </summary>
/// <remarks>
/// https://learn.microsoft.com/ef/core/cli/dbcontext-creation#from-a-design-time-factory
/// </remarks>
internal sealed class OracleOrderingDbContextFactory : IDesignTimeDbContextFactory<OracleOrderingDbContext>
{
    public OracleOrderingDbContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<OracleOrderingDbContext>()
            .UseOracle("User Id=design;Password=design;Data Source=localhost:1521/FREEPDB1", oracle => oracle
                .UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion23)
                .MigrationsHistoryTable("__EFMigrationsHistory", ModuleSchema.For(OrderingDbContext.Module, "Oracle")))
            .Options,
        DesignTimeDomainEventDispatcher.Instance);
}

internal sealed class PostgreSqlOrderingDbContextFactory : IDesignTimeDbContextFactory<PostgreSqlOrderingDbContext>
{
    public PostgreSqlOrderingDbContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<PostgreSqlOrderingDbContext>()
            .UseNpgsql("Host=localhost;Database=design", npgsql => npgsql
                .MigrationsHistoryTable("__EFMigrationsHistory", ModuleSchema.For(OrderingDbContext.Module, "Npgsql")))
            .Options,
        DesignTimeDomainEventDispatcher.Instance);
}
