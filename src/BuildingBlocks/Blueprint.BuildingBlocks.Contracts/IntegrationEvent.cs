namespace Blueprint.BuildingBlocks.Contracts;

/// <summary>
/// Basis für Integration Events: modulübergreifende (später service-übergreifende) Benachrichtigungen,
/// die asynchron über den Event Bus zugestellt werden.
/// </summary>
/// <remarks>
/// Integration Events sind Teil des öffentlichen Vertrags eines Moduls und müssen abwärtskompatibel weiterentwickelt werden.
/// Siehe "Implementing event-based communication between microservices (integration events)":
/// https://learn.microsoft.com/dotnet/architecture/microservices/multi-container-microservice-net-applications/integration-event-based-microservice-communications
/// </remarks>
public abstract record IntegrationEvent
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}
