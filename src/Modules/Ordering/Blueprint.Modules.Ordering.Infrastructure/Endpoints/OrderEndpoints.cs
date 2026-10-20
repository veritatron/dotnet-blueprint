using System.Security.Claims;
using Blueprint.BuildingBlocks.Application.Messaging;
using Blueprint.BuildingBlocks.Infrastructure.Web;
using Blueprint.Modules.Ordering.Application.Orders;
using Blueprint.Modules.Ordering.Contracts;
using Blueprint.Modules.Ordering.Contracts.Orders;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Blueprint.Modules.Ordering.Infrastructure.Endpoints;

/// <summary>HTTP-Endpunkte des Ordering-Moduls.</summary>
/// <remarks>
/// Asynchrone Verarbeitung → <c>202 Accepted</c>: https://learn.microsoft.com/azure/architecture/patterns/async-request-reply
/// </remarks>
internal static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(OrderingRoutes.Orders)
            .WithTags("Ordering")
            .WithGroupName(OrderingModule.Name)
            .RequireAuthorization();

        group.MapGet("/", GetMyOrdersAsync).WithName("GetMyOrders").WithSummary("Liefert die Bestellungen des angemeldeten Benutzers.");
        group.MapGet("/{id:guid}", GetOrderByIdAsync)
            .WithName("GetOrderById")
            .WithSummary("Liefert eine Bestellung.")
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPost("/", PlaceOrderAsync)
            .WithName("PlaceOrder")
            .WithSummary("Gibt eine Bestellung auf (Bestandsreservierung erfolgt asynchron).")
            .ProducesValidationProblem();

        return endpoints;
    }

    private static async Task<Results<Ok<IReadOnlyList<OrderResponse>>, ProblemHttpResult>> GetMyOrdersAsync(
        ClaimsPrincipal user,
        IQueryHandler<GetMyOrdersQuery, IReadOnlyList<OrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetMyOrdersQuery(user.GetCustomerId()), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<Results<Ok<OrderResponse>, ProblemHttpResult>> GetOrderByIdAsync(
        Guid id,
        ClaimsPrincipal user,
        IQueryHandler<GetOrderByIdQuery, OrderResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetOrderByIdQuery(id, user.GetCustomerId()), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<Results<Accepted<OrderResponse>, ProblemHttpResult>> PlaceOrderAsync(
        PlaceOrderRequest request,
        ClaimsPrincipal user,
        ICommandHandler<PlaceOrderCommand, OrderResponse> handler,
        CancellationToken cancellationToken)
    {
        var command = new PlaceOrderCommand(
            user.GetCustomerId(),
            request.Lines.Select(line => (line.ProductId, line.Quantity)).ToList());

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Accepted($"/{OrderingRoutes.Orders}/{result.Value.Id}", result.Value)
            : result.Error.ToProblem();
    }

    /// <summary>Kunden-ID = Subject-Claim des Access Tokens (stabil, eindeutig pro Identity Provider).</summary>
    private static string GetCustomerId(this ClaimsPrincipal user) =>
        user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Access Token enthält keinen 'sub'-Claim.");
}
