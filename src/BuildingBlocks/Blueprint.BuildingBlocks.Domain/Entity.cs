namespace Blueprint.BuildingBlocks.Domain;

/// <summary>
/// Basisklasse für Entitäten mit Identität.
/// </summary>
/// <remarks>
/// Siehe "Design a microservice domain model":
/// https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/microservice-domain-model
/// </remarks>
public abstract class Entity<TId>
    where TId : notnull
{
    protected Entity(TId id) => Id = id;

    /// <summary>Konstruktor für EF Core (Materialisierung).</summary>
    protected Entity()
    {
        Id = default!;
    }

    public TId Id { get; protected init; }
}
