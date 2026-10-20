using Blueprint.BuildingBlocks.Contracts;

namespace Blueprint.Modules.Catalog.Contracts.IntegrationEvents;

/// <summary>Der Bestand für alle Positionen einer Bestellung wurde reserviert.</summary>
public sealed record StockReservedIntegrationEvent(Guid OrderId) : IntegrationEvent;

/// <summary>Der Bestand für eine Bestellung konnte nicht reserviert werden.</summary>
public sealed record StockReservationFailedIntegrationEvent(Guid OrderId, string Reason) : IntegrationEvent;
