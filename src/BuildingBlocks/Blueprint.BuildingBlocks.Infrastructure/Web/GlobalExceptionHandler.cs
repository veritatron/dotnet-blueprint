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
        LogUnhandledException(exception, httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Title = "Ein unerwarteter Fehler ist aufgetreten.",
                Status = StatusCodes.Status500InternalServerError,
            },
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unbehandelte Exception bei {Path}")]
    private partial void LogUnhandledException(Exception exception, string path);
}
