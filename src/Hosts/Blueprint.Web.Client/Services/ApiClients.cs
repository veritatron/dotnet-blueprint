using System.Net.Http.Json;
using Blueprint.Modules.Catalog.Contracts;
using Blueprint.Modules.Catalog.Contracts.Products;
using Blueprint.Modules.Ordering.Contracts;
using Blueprint.Modules.Ordering.Contracts.Orders;

namespace Blueprint.Web.Client.Services;

/// <summary>
/// Typed HTTP-Clients für die Modul-APIs. Dieselben Klassen laufen im Browser (Basisadresse = BFF)
/// und beim Prerendering auf dem Server (Basisadresse = API, Token per DelegatingHandler).
/// </summary>
/// <remarks>
/// https://learn.microsoft.com/aspnet/core/blazor/call-web-api#typed-httpclient
/// </remarks>
public static class ApiClientRegistration
{
    public static IServiceCollection AddApiClients(this IServiceCollection services, Uri baseAddress, Action<IHttpClientBuilder>? configure = null)
    {
        var catalog = services.AddHttpClient<CatalogApiClient>(client => client.BaseAddress = baseAddress);
        var ordering = services.AddHttpClient<OrderingApiClient>(client => client.BaseAddress = baseAddress);
        configure?.Invoke(catalog);
        configure?.Invoke(ordering);
        return services;
    }
}

public sealed class CatalogApiClient(HttpClient httpClient)
{
    public async Task<List<ProductResponse>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(CatalogRoutes.Products, cancellationToken);
        return await response.ReadAsync<List<ProductResponse>>(cancellationToken) ?? [];
    }

    public async Task<ProductResponse?> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(CatalogRoutes.Products, request, cancellationToken);
        return await response.ReadAsync<ProductResponse>(cancellationToken);
    }
}

public sealed class OrderingApiClient(HttpClient httpClient)
{
    public async Task<List<OrderResponse>> GetMyOrdersAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(OrderingRoutes.Orders, cancellationToken);
        return await response.ReadAsync<List<OrderResponse>>(cancellationToken) ?? [];
    }

    public async Task<OrderResponse?> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(OrderingRoutes.Orders, request, cancellationToken);
        return await response.ReadAsync<OrderResponse>(cancellationToken);
    }
}

/// <summary>Fehler der API (aus Problem Details, RFC 9457).</summary>
public sealed class ApiException(string message) : Exception(message);

/// <summary>Minimale Sicht auf Problem Details (die MVC-Klasse ist in WebAssembly nicht verfügbar).</summary>
internal sealed record ProblemResponse(string? Title, string? Detail);

internal static class HttpResponseMessageExtensions
{
    public static async Task<T?> ReadAsync<T>(this HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        }

        var problem = response.Content.Headers.ContentType?.MediaType == "application/problem+json"
            ? await response.Content.ReadFromJsonAsync<ProblemResponse>(cancellationToken)
            : null;

        throw new ApiException(problem?.Detail ?? problem?.Title ?? $"Anfrage fehlgeschlagen ({(int)response.StatusCode}).");
    }
}
