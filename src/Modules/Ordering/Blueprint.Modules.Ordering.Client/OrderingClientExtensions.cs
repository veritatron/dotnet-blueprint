using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Blueprint.Modules.Ordering.Client;

public static class OrderingClientExtensions
{
    /// <summary>Registriert den generierten Refit-Client <see cref="IOrderingApi"/> über <c>IHttpClientFactory</c>.</summary>
    /// <remarks>https://github.com/reactiveui/refit#using-httpclientfactory</remarks>
    public static IHttpClientBuilder AddOrderingApiClient(this IServiceCollection services, Uri baseAddress) =>
        services.AddRefitGeneratedClient<IOrderingApi>()
            .ConfigureHttpClient(client => client.BaseAddress = baseAddress);
}
