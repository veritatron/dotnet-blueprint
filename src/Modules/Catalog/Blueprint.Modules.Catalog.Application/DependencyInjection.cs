using Blueprint.BuildingBlocks.Application.Messaging;
using Blueprint.Modules.Catalog.Application.Products;
using Blueprint.Modules.Catalog.Application.PublicApi;
using Blueprint.Modules.Catalog.Contracts;
using Blueprint.Modules.Catalog.Contracts.Products;
using Microsoft.Extensions.DependencyInjection;

namespace Blueprint.Modules.Catalog.Application;

/// <summary>Registriert die Use Cases des Catalog-Moduls.</summary>
/// <remarks>
/// Explizite Registrierung statt Assembly-Scanning – nachvollziehbar und trimming-sicher:
/// https://learn.microsoft.com/dotnet/core/extensions/dependency-injection-guidelines
/// </remarks>
public static class DependencyInjection
{
    public static IServiceCollection AddCatalogApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateProductCommand, ProductResponse>, CreateProductCommandHandler>();
        services.AddScoped<IQueryHandler<GetProductsQuery, IReadOnlyList<ProductResponse>>, GetProductsQueryHandler>();
        services.AddScoped<IQueryHandler<GetProductByIdQuery, ProductResponse>, GetProductByIdQueryHandler>();
        services.AddScoped<IQueryHandler<GetProductSnapshotsQuery, IReadOnlyCollection<ProductSnapshot>>, GetProductSnapshotsQueryHandler>();
        services.AddScoped<ICatalogModuleApi, CatalogModuleApi>();
        return services;
    }
}
