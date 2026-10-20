using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;

namespace Blueprint.Web.Bff;

/// <summary>
/// Hängt beim serverseitigen Prerendering das Access Token der aktuellen Cookie-Session an API-Aufrufe an.
/// </summary>
/// <remarks>
/// IHttpContextAccessor ist hier zulässig, da der Server nur statisch prerendert (keine interaktiven Server-Circuits):
/// https://learn.microsoft.com/aspnet/core/blazor/components/httpcontext<br/>
/// DelegatingHandler: https://learn.microsoft.com/aspnet/core/fundamentals/http-requests#outgoing-request-middleware
/// </remarks>
internal sealed class ServerAccessTokenHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("Kein HttpContext verfügbar (nur beim Prerendering unterstützt).");

        var accessToken = await httpContext.GetTokenAsync("access_token");
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
