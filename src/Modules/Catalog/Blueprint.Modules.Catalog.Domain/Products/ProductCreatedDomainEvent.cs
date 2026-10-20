using Blueprint.BuildingBlocks.Domain;

namespace Blueprint.Modules.Catalog.Domain.Products;

public sealed record ProductCreatedDomainEvent(Guid ProductId) : IDomainEvent;
