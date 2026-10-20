using Blueprint.Modules.Catalog.Client;
using Blueprint.Modules.Ordering.Client;

namespace Blueprint.Web.Client.Services;

/// <summary>
/// Registriert die generierten Refit-Clients. Im Browser zeigen sie auf den BFF (gleiche Origin, Cookie),
/// beim Prerendering auf dem Server direkt auf die API (Access Token per DelegatingHandler).
/// </summary>
/// <remarks>
/// https://learn.microsoft.com/aspnet/core/blazor/call-web-api
/// </remarks>
public static class ApiClientRegistration
{
    public static IServiceCollection AddBlueprintApiClients(this IServiceCollection services, Uri baseAddress, Action<IHttpClientBuilder>? configure = null)
    {
        var catalog = services.AddCatalogApiClient(baseAddress);
        var ordering = services.AddOrderingApiClient(baseAddress);
        configure?.Invoke(catalog);
        configure?.Invoke(ordering);
        return services;
    }
}
