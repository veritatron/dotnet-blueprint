using Blueprint.BuildingBlocks.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Blueprint.BuildingBlocks.Infrastructure.Web;

/// <summary>Bildet fachliche Fehler auf RFC 9457 Problem Details ab.</summary>
/// <remarks>
/// Problem Details in ASP.NET Core: https://learn.microsoft.com/aspnet/core/fundamentals/error-handling-api#problem-details<br/>
/// TypedResults: https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/responses#typedresults-vs-results
/// </remarks>
public static class ResultExtensions
{
    public static ProblemHttpResult ToProblem(this Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

        return TypedResults.Problem(
            statusCode: statusCode,
            title: error.Code,
            detail: error.Description,
            extensions: new Dictionary<string, object?> { ["errorCode"] = error.Code });
    }
}
