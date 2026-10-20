using Blueprint.BuildingBlocks.Domain;

namespace Blueprint.BuildingBlocks.Application.Messaging;

/// <summary>Command: ändert Zustand (Write-Seite, CQRS).</summary>
/// <remarks>
/// CQRS in Microservices: https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/apply-simplified-microservice-cqrs-ddd-patterns<br/>
/// Bewusst ohne Mediator-Bibliothek: Endpoints rufen ihren Handler direkt über Dependency Injection auf (explizit, AOT-/trimming-freundlich).
/// </remarks>
public interface ICommand<TResponse>;

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
