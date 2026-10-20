using Blueprint.BuildingBlocks.Contracts;

namespace Blueprint.Modules.Ordering.Contracts.IntegrationEvents;

/// <summary>Eine Bestellung wurde aufgegeben und wartet auf Bestandsreservierung.</summary>
public sealed record OrderPlacedIntegrationEvent(Guid OrderId, string CustomerId, IReadOnlyList<OrderPlacedLine> Lines) : IntegrationEvent;

public sealed record OrderPlacedLine(Guid ProductId, int Quantity);
