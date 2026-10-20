using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Blueprint.BuildingBlocks.Infrastructure.Web;

/// <summary>
/// Zentrale Behandlung unerwarteter Exceptions → Problem Details (ohne interne Details nach außen zu geben).
/// </summary>
/// <remarks>
/// IExceptionHandler: https://learn.microsoft.com/aspnet/core/fundamentals/error-handling-api#iexceptionhandler
/// </remarks>
public sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Fehlerhafte Requests (z. B. ungültiges JSON) sind Client-Fehler und behalten ihren Statuscode (400).
        // Remarks: https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/handle-errors
        var (status, title) = exception is BadHttpRequestException badRequest
            ? (badRequest.StatusCode, "Die Anfrage ist ungültig.")
            : (StatusCodes.Status500InternalServerError, "Ein unerwarteter Fehler ist aufgetreten.");

        if (status >= StatusCodes.Status500InternalServerError)
        {
            LogUnhandledException(exception, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Title = title,
                Status = status,
            },
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unbehandelte Exception bei {Path}")]
    private partial void LogUnhandledException(Exception exception, string path);
}
