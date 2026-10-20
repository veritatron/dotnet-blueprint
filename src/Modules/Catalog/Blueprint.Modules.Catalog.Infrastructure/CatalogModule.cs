using Blueprint.BuildingBlocks.Infrastructure.Messaging;
using Blueprint.BuildingBlocks.Infrastructure.Persistence;
using Blueprint.Modules.Catalog.Application;
using Blueprint.Modules.Catalog.Application.Abstractions;
using Blueprint.Modules.Catalog.Application.IntegrationEvents;
using Blueprint.Modules.Catalog.Infrastructure.Endpoints;
using Blueprint.Modules.Catalog.Infrastructure.Persistence;
using Blueprint.Modules.Ordering.Contracts.IntegrationEvents;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Blueprint.Modules.Catalog.Infrastructure;

/// <summary>
/// Composition Root des Catalog-Moduls. Ein Host (Modulith-API ODER eigener Microservice) ruft nur
/// <see cref="AddCatalogModule"/> und <see cref="MapCatalogModule"/> auf.
/// </summary>
/// <remarks>
/// Modulare Monolithen / Auslagerung in Microservices:
/// https://learn.microsoft.com/azure/architecture/microservices/migrate-monolith<br/>
/// Erweiterungsmethoden für Service-Registrierung: https://learn.microsoft.com/dotnet/core/extensions/dependency-injection-usage#register-groups-of-services-with-extension-methods
/// </remarks>
public static class CatalogModule
{
    public const string AdminPolicy = "catalog-admin";

    public static IHostApplicationBuilder AddCatalogModule(this IHostApplicationBuilder builder)
    {
        builder.AddModuleDbContext<CatalogDbContext, OracleCatalogDbContext, PostgreSqlCatalogDbContext>(CatalogDbContext.Module);

        builder.Services.AddScoped<ICatalogUnitOfWork>(sp => sp.GetRequiredService<CatalogDbContext>());
        builder.Services.AddScoped<IProductRepository, ProductRepository>();
        builder.Services.AddCatalogApplication();

        builder.Services.AddIntegrationEventHandler<OrderPlacedIntegrationEvent, OrderPlacedIntegrationEventHandler, CatalogDbContext>(CatalogDbContext.Module);

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(AdminPolicy, policy => policy.RequireAuthenticatedUser().RequireRole("catalog-admin"));

        // .NET 10: eingebaute Validierung (DataAnnotations) für Minimal APIs dieses Moduls.
        builder.Services.AddValidation();

        return builder;
    }

    public static IEndpointRouteBuilder MapCatalogModule(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapProductEndpoints();
}
