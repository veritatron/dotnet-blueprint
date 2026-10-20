using Refit;

namespace Blueprint.Web.Client.Services;

/// <summary>Liest Problem Details (RFC 9457) aus einer fehlgeschlagenen Refit-Antwort.</summary>
/// <remarks>https://learn.microsoft.com/aspnet/core/fundamentals/error-handling-api#problem-details</remarks>
public static class ApiErrors
{
    public static async Task<string> GetMessageAsync(ApiException exception)
    {
        Problem? problem = null;
        try
        {
            problem = await exception.GetContentAsAsync<Problem>();
        }
        catch (Exception) when (exception.Content is null or "")
        {
            // Kein (JSON-)Body vorhanden.
        }

        var validation = problem?.Errors?.SelectMany(error => error.Value).ToList();
        return validation is { Count: > 0 }
            ? string.Join(" ", validation)
            : problem?.Detail ?? problem?.Title ?? $"Anfrage fehlgeschlagen ({(int)exception.StatusCode}).";
    }

    private sealed record Problem(string? Title, string? Detail, Dictionary<string, string[]>? Errors);
}
