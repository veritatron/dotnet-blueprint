using Blueprint.Modules.Catalog.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Blueprint.Modules.Catalog.Client;

public static class CatalogClientExtensions
{
    /// <summary>
    /// Registriert den generierten Refit-Client <see cref="ICatalogApi"/> über <c>IHttpClientFactory</c>.
    /// </summary>
    /// <param name="services">Service Collection.</param>
    /// <param name="baseAddress">Basisadresse, z. B. die des BFF im Browser oder <c>https+http://catalog</c> (Service Discovery).</param>
    /// <remarks>
    /// Resilience und Service Discovery kommen in Server-Hosts aus den ServiceDefaults (<c>ConfigureHttpClientDefaults</c>):
    /// https://learn.microsoft.com/dotnet/core/resilience/http-resilience ·
    /// https://learn.microsoft.com/dotnet/core/extensions/service-discovery<br/>
    /// Refit + HttpClientFactory (quellgenerierte, reflection-freie Implementierung – trimming-/WebAssembly-tauglich):
    /// https://github.com/reactiveui/refit#using-httpclientfactory
    /// </remarks>
    public static IHttpClientBuilder AddCatalogApiClient(this IServiceCollection services, Uri baseAddress) =>
        services.AddRefitGeneratedClient<ICatalogApi>()
            .ConfigureHttpClient(client => client.BaseAddress = baseAddress);

    /// <summary>
    /// Registriert <see cref="ICatalogModuleApi"/> als HTTP-Implementierung auf Basis des generierten Clients –
    /// für Module/Services, die nach einer Auslagerung des Catalog-Moduls dessen Daten benötigen.
    /// </summary>
    public static IHttpClientBuilder AddCatalogModuleHttpClient(this IServiceCollection services, Uri baseAddress)
    {
        services.AddScoped<ICatalogModuleApi, CatalogModuleHttpClient>();
        return services.AddCatalogApiClient(baseAddress);
    }
}

/// <summary>Adapter: öffentliche Modul-API → generierter Refit-Client.</summary>
internal sealed class CatalogModuleHttpClient(ICatalogApi api) : ICatalogModuleApi
{
    public async Task<IReadOnlyCollection<ProductSnapshot>> GetProductsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
        {
            return [];
        }

        var snapshots = await api.GetProductSnapshotsAsync(productIds, cancellationToken);
        return snapshots
            .Select(s => new ProductSnapshot(s.Id, s.Sku, s.Name, s.UnitPrice, s.Currency, s.AvailableStock))
            .ToList();
    }
}
