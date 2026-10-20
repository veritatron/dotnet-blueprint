using Blueprint.BuildingBlocks.Application.Messaging;
using Blueprint.BuildingBlocks.Infrastructure.Web;
using Blueprint.Modules.Catalog.Application.Products;
using Blueprint.Modules.Catalog.Contracts;
using Blueprint.Modules.Catalog.Contracts.Products;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Blueprint.Modules.Catalog.Infrastructure.Endpoints;

/// <summary>HTTP-Endpunkte des Catalog-Moduls (Minimal APIs mit TypedResults).</summary>
/// <remarks>
/// <list type="bullet">
/// <item>Minimal APIs: https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/overview</item>
/// <item>Route Groups: https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/route-handlers#route-groups</item>
/// <item>OpenAPI-Metadaten: https://learn.microsoft.com/aspnet/core/fundamentals/openapi/include-metadata</item>
/// </list>
/// </remarks>
internal static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(CatalogRoutes.Products)
            .WithTags("Catalog")
            .RequireAuthorization();

        group.MapGet("/", GetProductsAsync).WithName("GetProducts").WithSummary("Liefert alle Produkte.");
        group.MapGet("/{id:guid}", GetProductByIdAsync).WithName("GetProductById").WithSummary("Liefert ein Produkt.");
        group.MapPost("/", CreateProductAsync)
            .WithName("CreateProduct")
            .WithSummary("Legt ein Produkt an.")
            .RequireAuthorization(CatalogModule.AdminPolicy);

        // Wird vom Catalog-Modul-Client (HTTP-Implementierung von ICatalogModuleApi) nach einer Auslagerung verwendet.
        group.MapGet("/snapshots", GetProductSnapshotsAsync)
            .WithName("GetProductSnapshots")
            .WithSummary("Liefert schlanke Produktdaten für andere Services.");

        return endpoints;
    }

    private static async Task<Results<Ok<IReadOnlyList<ProductResponse>>, ProblemHttpResult>> GetProductsAsync(
        IQueryHandler<GetProductsQuery, IReadOnlyList<ProductResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetProductsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<Results<Ok<ProductResponse>, ProblemHttpResult>> GetProductByIdAsync(
        Guid id,
        IQueryHandler<GetProductByIdQuery, ProductResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetProductByIdQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<Results<Created<ProductResponse>, ProblemHttpResult>> CreateProductAsync(
        CreateProductRequest request,
        ICommandHandler<CreateProductCommand, ProductResponse> handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand(request.Sku, request.Name, request.Description, request.Price, request.Currency, request.InitialStock);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/{CatalogRoutes.Products}/{result.Value.Id}", result.Value)
            : result.Error.ToProblem();
    }

    private static async Task<Results<Ok<IReadOnlyCollection<ProductSnapshot>>, ProblemHttpResult>> GetProductSnapshotsAsync(
        [FromQuery(Name = "id")] Guid[] ids,
        IQueryHandler<GetProductSnapshotsQuery, IReadOnlyCollection<ProductSnapshot>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetProductSnapshotsQuery(ids), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}
