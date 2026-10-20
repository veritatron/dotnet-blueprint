namespace Blueprint.BuildingBlocks.Domain;

/// <summary>
/// Aggregate Root: Konsistenzgrenze und einziger Einstiegspunkt in ein Aggregat.
/// Sammelt Domain Events, die beim Speichern (in derselben Transaktion) dispatcht werden.
/// </summary>
/// <remarks>
/// Aggregate-Pattern: https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/net-core-microservice-domain-model<br/>
/// Domain Events: https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation
/// </remarks>
public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    protected AggregateRoot()
    {
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
