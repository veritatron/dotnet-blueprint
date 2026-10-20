using System.Net.Http.Json;
using Blueprint.Modules.Catalog.Contracts;

namespace Blueprint.Modules.Catalog.Client;

/// <summary>Remote-Implementierung von <see cref="ICatalogModuleApi"/> über HTTP (Typed Client).</summary>
/// <remarks>
/// Typed Clients mit IHttpClientFactory: https://learn.microsoft.com/dotnet/core/extensions/httpclient-factory#typed-clients
/// </remarks>
internal sealed class CatalogModuleHttpClient(HttpClient httpClient) : ICatalogModuleApi
{
    public async Task<IReadOnlyCollection<ProductSnapshot>> GetProductsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
        {
            return [];
        }

        var query = string.Join('&', productIds.Select(id => $"id={id}"));
        return await httpClient.GetFromJsonAsync<List<ProductSnapshot>>($"{CatalogRoutes.ProductSnapshots}?{query}", cancellationToken) ?? [];
    }
}
