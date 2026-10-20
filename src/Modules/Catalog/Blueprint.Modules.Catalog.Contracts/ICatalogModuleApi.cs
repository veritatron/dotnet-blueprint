namespace Blueprint.Modules.Catalog.Contracts;

/// <summary>
/// Synchrone Modul-API des Catalog-Moduls für andere Module.
/// Im Modulith: In-Process-Implementierung. Nach Auslagerung: HTTP-Implementierung
/// (<c>Blueprint.Modules.Catalog.Client</c>) – Aufrufer bleiben unverändert.
/// </summary>
/// <remarks>
/// Synchrone vs. asynchrone Kommunikation:
/// https://learn.microsoft.com/dotnet/architecture/microservices/architect-microservice-container-applications/communication-in-microservice-architecture<br/>
/// Hinweis: Synchrone Aufrufe zwischen Services sparsam einsetzen; wo möglich Daten per Integration Event replizieren.
/// </remarks>
public interface ICatalogModuleApi
{
    Task<IReadOnlyCollection<ProductSnapshot>> GetProductsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken = default);
}

/// <summary>Schlanke, stabile Sicht auf ein Produkt für andere Module.</summary>
public sealed record ProductSnapshot(Guid Id, string Sku, string Name, decimal UnitPrice, string Currency, int AvailableStock);
