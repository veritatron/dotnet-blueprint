using Blueprint.Modules.Catalog.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Blueprint.Modules.Catalog.Client;

public static class CatalogClientExtensions
{
    /// <summary>
    /// Registriert <see cref="ICatalogModuleApi"/> als HTTP-Client gegen einen ausgelagerten Catalog-Service.
    /// </summary>
    /// <param name="services">Service Collection.</param>
    /// <param name="baseAddress">Basisadresse, z. B. <c>https+http://catalog-api</c> (Aspire Service Discovery).</param>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Standard-Resilience-Handler (Retry, Circuit Breaker, Timeout): https://learn.microsoft.com/dotnet/core/resilience/http-resilience</item>
    /// <item>Service Discovery: https://learn.microsoft.com/dotnet/core/extensions/service-discovery</item>
    /// <item>Weitergabe des Benutzer-Tokens (Header Propagation): https://learn.microsoft.com/aspnet/core/fundamentals/http-requests#header-propagation-middleware</item>
    /// </list>
    /// </remarks>
    public static IHttpClientBuilder AddCatalogModuleHttpClient(this IServiceCollection services, Uri baseAddress)
    {
        var builder = services.AddHttpClient<ICatalogModuleApi, CatalogModuleHttpClient>(client => client.BaseAddress = baseAddress);
        builder.AddServiceDiscovery();
        builder.AddStandardResilienceHandler();
        return builder;
    }
}
