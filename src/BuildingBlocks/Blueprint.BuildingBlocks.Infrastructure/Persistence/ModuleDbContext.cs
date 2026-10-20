using Blueprint.BuildingBlocks.Application.Persistence;
using Blueprint.BuildingBlocks.Contracts;
using Blueprint.BuildingBlocks.Domain;
using Blueprint.BuildingBlocks.Infrastructure.DomainEvents;
using Blueprint.BuildingBlocks.Infrastructure.Messaging.Inbox;
using Blueprint.BuildingBlocks.Infrastructure.Messaging.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Blueprint.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Basis-DbContext für ein Modul. Jedes Modul besitzt einen eigenen DbContext und ein eigenes Datenbankschema –
/// auch wenn sich zunächst alle Module eine physische Datenbank teilen. Dadurch kann ein Modul später ohne
/// Änderungen am Datenmodell in eine eigene Datenbank (Microservice) verschoben werden.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Ein DbContext pro Bounded Context / Microservice: https://learn.microsoft.com/dotnet/architecture/microservices/architect-microservice-container-applications/data-sovereignty-per-microservice</item>
/// <item>DbContext als Unit of Work: https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-implementation-entity-framework-core</item>
/// <item>Domain Events vor dem Commit dispatchen: https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation#the-deferred-approach-to-raise-and-dispatch-events</item>
/// <item>Transactional Outbox: https://learn.microsoft.com/azure/architecture/databases/guide/transactional-outbox-cosmos</item>
/// </list>
/// </remarks>
public abstract class ModuleDbContext(DbContextOptions options, IDomainEventDispatcher domainEventDispatcher)
    : DbContext(options), IUnitOfWork, IIntegrationEventOutbox
{
    /// <summary>Logischer Modulname, z. B. <c>catalog</c>. Wird als Schemaname verwendet.</summary>
    public abstract string ModuleName { get; }

    /// <summary>Name der Shadow Property für optimistische Nebenläufigkeit.</summary>
    public const string ConcurrencyTokenProperty = "Version";

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    public void Enqueue(IntegrationEvent integrationEvent) =>
        OutboxMessages.Add(OutboxMessage.Create(integrationEvent));

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync(cancellationToken);
        UpdateConcurrencyTokens();
        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(ModuleSchema.For(ModuleName, Database.ProviderName));
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration(ModuleSchema.IsOracle(Database.ProviderName)));
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
    }

    /// <summary>
    /// Setzt eine neue <c>Version</c> (Shadow Property) für geänderte Entitäten – provider-neutrale,
    /// app-verwaltete optimistische Nebenläufigkeit (funktioniert identisch auf Oracle und PostgreSQL).
    /// </summary>
    /// <remarks>
    /// https://learn.microsoft.com/ef/core/saving/concurrency#application-managed-concurrency-tokens
    /// </remarks>
    private void UpdateConcurrencyTokens()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is EntityState.Added or EntityState.Modified
                && entry.Metadata.FindProperty(ConcurrencyTokenProperty) is not null)
            {
                entry.Property(ConcurrencyTokenProperty).CurrentValue = Guid.CreateVersion7();
            }
        }
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        // Handler können weitere Aggregate ändern und damit neue Domain Events auslösen –
        // daher so lange dispatchen, bis keine Events mehr anstehen.
        while (true)
        {
            var aggregates = ChangeTracker.Entries<IHasDomainEvents>()
                .Select(entry => entry.Entity)
                .Where(entity => entity.DomainEvents.Count != 0)
                .ToList();

            if (aggregates.Count == 0)
            {
                return;
            }

            var domainEvents = aggregates.SelectMany(aggregate => aggregate.DomainEvents).ToList();
            aggregates.ForEach(aggregate => aggregate.ClearDomainEvents());

            foreach (var domainEvent in domainEvents)
            {
                await domainEventDispatcher.DispatchAsync(domainEvent, cancellationToken);
            }
        }
    }
}
