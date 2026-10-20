using Blueprint.BuildingBlocks.Infrastructure.DomainEvents;
using Blueprint.BuildingBlocks.Infrastructure.Persistence;
using Blueprint.Modules.Catalog.Application.Abstractions;
using Blueprint.Modules.Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Blueprint.Modules.Catalog.Infrastructure.Persistence;

/// <summary>
/// Provider-neutraler DbContext des Catalog-Moduls (Schema <c>catalog</c>).
/// Die abgeleiteten Klassen <see cref="OracleCatalogDbContext"/> und <see cref="PostgreSqlCatalogDbContext"/>
/// existieren nur, damit jeder Provider seine eigenen Migrations erhält.
/// </summary>
public class CatalogDbContext(DbContextOptions options, IDomainEventDispatcher domainEventDispatcher)
    : ModuleDbContext(options, domainEventDispatcher), ICatalogUnitOfWork
{
    public const string Module = "catalog";

    public override string ModuleName => Module;

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}

/// <summary>Oracle-spezifischer Kontext (Migrations unter <c>Persistence/Migrations/Oracle</c>).</summary>
public sealed class OracleCatalogDbContext(DbContextOptions<OracleCatalogDbContext> options, IDomainEventDispatcher domainEventDispatcher)
    : CatalogDbContext(options, domainEventDispatcher);

/// <summary>PostgreSQL-spezifischer Kontext (Migrations unter <c>Persistence/Migrations/PostgreSql</c>).</summary>
public sealed class PostgreSqlCatalogDbContext(DbContextOptions<PostgreSqlCatalogDbContext> options, IDomainEventDispatcher domainEventDispatcher)
    : CatalogDbContext(options, domainEventDispatcher);
