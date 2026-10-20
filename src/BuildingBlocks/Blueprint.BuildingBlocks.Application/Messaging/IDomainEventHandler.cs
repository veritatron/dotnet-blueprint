using Blueprint.BuildingBlocks.Domain;

namespace Blueprint.BuildingBlocks.Application.Messaging;

/// <summary>
/// Verarbeitet ein Domain Event in-process, innerhalb derselben Unit of Work (vor dem Commit).
/// </summary>
/// <remarks>
/// https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation#the-deferred-approach-to-raise-and-dispatch-events
/// </remarks>
public interface IDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken);
}
