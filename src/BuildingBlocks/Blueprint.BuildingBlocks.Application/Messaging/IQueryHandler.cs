using Blueprint.BuildingBlocks.Domain;

namespace Blueprint.BuildingBlocks.Application.Messaging;

/// <summary>Query: liest Zustand, ohne ihn zu verändern (Read-Seite, CQRS).</summary>
/// <remarks>
/// Siehe https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/cqrs-microservice-reads
/// </remarks>
public interface IQuery<TResponse>;

public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
