namespace Blueprint.BuildingBlocks.Domain;

/// <summary>
/// Marker für Domain Events. Domain Events sind modul-intern und werden in-process, innerhalb
/// derselben Transaktion verarbeitet. Für modulübergreifende Kommunikation werden sie in
/// Integration Events übersetzt (siehe Outbox).
/// </summary>
/// <remarks>
/// Domain Events vs. Integration Events:
/// https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation#domain-events-versus-integration-events
/// </remarks>
public interface IDomainEvent;

/// <summary>Wird von Aggregaten implementiert, die Domain Events sammeln.</summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
