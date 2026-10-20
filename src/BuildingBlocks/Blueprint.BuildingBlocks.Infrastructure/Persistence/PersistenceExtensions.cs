using Blueprint.BuildingBlocks.Infrastructure.Messaging.Outbox;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Blueprint.BuildingBlocks.Infrastructure.Persistence;

public static class PersistenceExtensions
{
    /// <summary>
    /// Registriert den DbContext eines Moduls für den konfigurierten Provider inkl. Health Check und Outbox-Processor.
    /// </summary>
    /// <typeparam name="TContext">Provider-neutraler DbContext des Moduls (wird von Application/Infrastructure verwendet).</typeparam>
    /// <typeparam name="TOracleContext">Abgeleiteter Kontext, der die Oracle-Migrations besitzt.</typeparam>
    /// <typeparam name="TPostgreSqlContext">Abgeleiteter Kontext, der die PostgreSQL-Migrations besitzt.</typeparam>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Migrations für mehrere Provider ("Using multiple context types"): https://learn.microsoft.com/ef/core/managing-schemas/migrations/providers#using-multiple-context-types</item>
    /// <item>Eigene Migrations-History-Tabelle pro Schema: https://learn.microsoft.com/ef/core/managing-schemas/migrations/history-table</item>
    /// <item>Connection Resiliency: https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency</item>
    /// <item>Health Checks für EF Core: https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks#entity-framework-core-dbcontext-probe</item>
    /// </list>
    /// </remarks>
    public static IHostApplicationBuilder AddModuleDbContext<TContext, TOracleContext, TPostgreSqlContext>(
        this IHostApplicationBuilder builder,
        string moduleName)
        where TContext : ModuleDbContext
        where TOracleContext : TContext
        where TPostgreSqlContext : TContext
    {
        var options = builder.Configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();

        // Modul-spezifischer Connection String hat Vorrang → Modul kann ohne Codeänderung eine eigene DB bekommen.
        var connectionString = builder.Configuration.GetConnectionString(moduleName)
            ?? builder.Configuration.GetConnectionString(options.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Kein Connection String '{moduleName}' oder '{options.ConnectionStringName}' konfiguriert.");

        var schema = ModuleSchema.For(moduleName, options.Provider.ToString());

        switch (options.Provider)
        {
            case DatabaseProvider.Oracle:
                var compatibility = Enum.Parse<OracleSQLCompatibility>(options.OracleSqlCompatibility, ignoreCase: true);
                builder.Services.AddDbContext<TContext, TOracleContext>(db => db.UseOracle(
                    connectionString,
                    oracle => oracle
                        .UseOracleSQLCompatibility(compatibility)
                        .MigrationsHistoryTable(HistoryRepository.DefaultTableName, schema)));
                break;

            case DatabaseProvider.PostgreSql:
                builder.Services.AddDbContext<TContext, TPostgreSqlContext>(db => db.UseNpgsql(
                    connectionString,
                    npgsql => npgsql
                        .EnableRetryOnFailure()
                        .MigrationsHistoryTable(HistoryRepository.DefaultTableName, schema)));
                break;

            default:
                throw new InvalidOperationException($"Unbekannter Datenbank-Provider '{options.Provider}'.");
        }

        builder.Services.AddSingleton(new ModuleDbContextRegistration(typeof(TContext), moduleName));
        builder.Services.AddHealthChecks().AddDbContextCheck<TContext>($"{moduleName}-db", tags: ["ready"]);
        builder.Services.AddOutboxProcessor<TContext>(builder.Configuration);

        return builder;
    }

    /// <summary>
    /// Wendet ausstehende Migrations aller Module an. Nur für die lokale Entwicklung gedacht!
    /// </summary>
    /// <remarks>
    /// Microsoft empfiehlt, Migrations in Produktion NICHT beim App-Start anzuwenden, sondern über
    /// SQL-Skripte (<c>dotnet ef migrations script --idempotent</c>) oder Migration Bundles im Deployment:
    /// https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying#apply-migrations-at-runtime
    /// </remarks>
    public static async Task MigrateModuleDatabasesAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        foreach (var registration in scope.ServiceProvider.GetServices<ModuleDbContextRegistration>())
        {
            var context = (DbContext)scope.ServiceProvider.GetRequiredService(registration.ContextType);
            await context.Database.MigrateAsync();
        }
    }

    private static class HistoryRepository
    {
        public const string DefaultTableName = "__EFMigrationsHistory";
    }
}

/// <summary>Merkt sich, welche Modul-DbContexts registriert sind (für Migrations/Diagnose).</summary>
public sealed record ModuleDbContextRegistration(Type ContextType, string ModuleName);
