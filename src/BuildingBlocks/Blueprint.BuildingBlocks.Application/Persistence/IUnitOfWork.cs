using Blueprint.BuildingBlocks.Contracts;

namespace Blueprint.BuildingBlocks.Application.Persistence;

/// <summary>Unit of Work eines Moduls. Jedes Modul definiert ein eigenes, abgeleitetes Interface.</summary>
/// <remarks>
/// https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design#the-unit-of-work-pattern
/// </remarks>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Transactional Outbox: Integration Events werden in derselben Transaktion wie die fachlichen Änderungen
/// gespeichert und erst danach (asynchron) veröffentlicht.
/// </summary>
/// <remarks>
/// https://learn.microsoft.com/dotnet/architecture/microservices/multi-container-microservice-net-applications/subscribe-events#designing-atomicity-and-resiliency-when-publishing-to-the-event-bus<br/>
/// https://learn.microsoft.com/azure/architecture/databases/guide/transactional-outbox-cosmos
/// </remarks>
public interface IIntegrationEventOutbox
{
    void Enqueue(IntegrationEvent integrationEvent);
}
