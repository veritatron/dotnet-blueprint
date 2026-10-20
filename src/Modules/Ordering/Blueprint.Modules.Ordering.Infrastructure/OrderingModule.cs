using Blueprint.BuildingBlocks.Infrastructure.Messaging;
using Blueprint.BuildingBlocks.Infrastructure.Persistence;
using Blueprint.BuildingBlocks.Infrastructure.Web;
using Blueprint.Modules.Catalog.Contracts.IntegrationEvents;
using Blueprint.Modules.Ordering.Application;
using Blueprint.Modules.Ordering.Application.Abstractions;
using Blueprint.Modules.Ordering.Application.IntegrationEvents;
using Blueprint.Modules.Ordering.Infrastructure.Endpoints;
using Blueprint.Modules.Ordering.Infrastructure.Persistence;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Blueprint.Modules.Ordering.Infrastructure;

/// <summary>
/// Composition Root des Ordering-Moduls.
/// Benötigt eine Implementierung von <c>ICatalogModuleApi</c>: im Modulith vom Catalog-Modul (in-process),
/// als Microservice über <c>AddCatalogModuleHttpClient(...)</c>.
/// </summary>
public static class OrderingModule
{
    /// <summary>Modulname (Schema, OpenAPI-Dokument, Gruppenname der Endpunkte).</summary>
    public const string Name = OrderingDbContext.Module;

    public static IHostApplicationBuilder AddOrderingModule(this IHostApplicationBuilder builder)
    {
        builder.AddModuleDbContext<OrderingDbContext, OracleOrderingDbContext, PostgreSqlOrderingDbContext>(OrderingDbContext.Module);

        builder.Services.AddScoped<IOrderingUnitOfWork>(sp => sp.GetRequiredService<OrderingDbContext>());
        builder.Services.AddScoped<IOrderRepository, OrderRepository>();
        builder.Services.AddOrderingApplication();

        builder.Services.AddIntegrationEventHandler<StockReservedIntegrationEvent, StockReservedIntegrationEventHandler, OrderingDbContext>(OrderingDbContext.Module);
        builder.Services.AddIntegrationEventHandler<StockReservationFailedIntegrationEvent, StockReservationFailedIntegrationEventHandler, OrderingDbContext>(OrderingDbContext.Module);

        builder.Services.AddValidation();

        // OpenAPI-Dokument des Moduls → Grundlage der generierten Refit-Clients (Blueprint.Modules.Ordering.Client).
        builder.Services.AddModuleOpenApi(Name, "Blueprint Ordering API");

        return builder;
    }

    public static IEndpointRouteBuilder MapOrderingModule(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapOrderEndpoints();
}
