using Blueprint.BuildingBlocks.Infrastructure.DomainEvents;
using Blueprint.BuildingBlocks.Infrastructure.Persistence;
using Blueprint.Modules.Ordering.Application.Abstractions;
using Blueprint.Modules.Ordering.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace Blueprint.Modules.Ordering.Infrastructure.Persistence;

/// <summary>Provider-neutraler DbContext des Ordering-Moduls (Schema <c>ordering</c>).</summary>
public class OrderingDbContext(DbContextOptions options, IDomainEventDispatcher domainEventDispatcher)
    : ModuleDbContext(options, domainEventDispatcher), IOrderingUnitOfWork
{
    public const string Module = "ordering";

    public override string ModuleName => Module;

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderingDbContext).Assembly);
    }
}

public sealed class OracleOrderingDbContext(DbContextOptions<OracleOrderingDbContext> options, IDomainEventDispatcher domainEventDispatcher)
    : OrderingDbContext(options, domainEventDispatcher);

public sealed class PostgreSqlOrderingDbContext(DbContextOptions<PostgreSqlOrderingDbContext> options, IDomainEventDispatcher domainEventDispatcher)
    : OrderingDbContext(options, domainEventDispatcher);
