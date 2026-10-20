using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Blueprint.Web.Bff;

/// <summary>Login/Logout-Endpunkte des BFF.</summary>
/// <remarks>
/// Nach dem Microsoft-Sample "BlazorWebAppOidcBff":
/// https://learn.microsoft.com/aspnet/core/blazor/security/blazor-web-app-with-oidc?pivots=with-yarp-and-aspire
/// </remarks>
internal static class LoginLogoutEndpoints
{
    public static IEndpointConventionBuilder MapLoginAndLogout(this IEndpointRouteBuilder endpoints, string oidcScheme)
    {
        var group = endpoints.MapGroup(string.Empty);

        group.MapGet("/login", (string? returnUrl) => TypedResults.Challenge(GetAuthProperties(returnUrl)))
            .AllowAnonymous();

        // POST + Antiforgery (automatisch für Formulare bei Minimal APIs) verhindert Logout-CSRF.
        group.MapPost("/logout", ([FromForm] string? returnUrl) => TypedResults.SignOut(
            GetAuthProperties(returnUrl),
            [CookieAuthenticationDefaults.AuthenticationScheme, oidcScheme]));

        return group;
    }

    /// <summary>Lässt nur relative Rücksprung-URLs zu (Schutz vor Open Redirects).</summary>
    /// <remarks>https://learn.microsoft.com/aspnet/core/security/preventing-open-redirects</remarks>
    private static AuthenticationProperties GetAuthProperties(string? returnUrl)
    {
        const string pathBase = "/";

        if (string.IsNullOrEmpty(returnUrl))
        {
            returnUrl = pathBase;
        }
        else if (!Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
        {
            returnUrl = new Uri(returnUrl, UriKind.Absolute).PathAndQuery;
        }
        else if (returnUrl[0] != '/')
        {
            returnUrl = $"{pathBase}{returnUrl}";
        }

        return new AuthenticationProperties { RedirectUri = returnUrl };
    }
}
