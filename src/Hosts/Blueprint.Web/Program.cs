using Blueprint.ServiceDefaults.Authentication;
using Blueprint.Web.Bff;
using Blueprint.Web.Client.Services;
using Blueprint.Web.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Yarp.ReverseProxy.Transforms;

const string OidcScheme = OpenIdConnectDefaults.AuthenticationScheme;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Adresse der Web API. "https+http://api" wird über Service Discovery aufgelöst (Aspire bzw. Konfiguration "Services:api").
// Remarks: https://learn.microsoft.com/dotnet/core/extensions/service-discovery
var apiBaseAddress = builder.Configuration["Bff:ApiBaseAddress"] ?? "https+http://api";

// ---------------------------------------------------------------------------------------------
// Authentifizierung: OIDC gegen Keycloak (Authorization Code + PKCE) mit Cookie-Session – Muster "BFF".
// Keycloak-Adresse per Service Discovery ("keycloak"), Realm/Client aus "Authentication:Keycloak",
// Client-Secret aus User Secrets / Umgebungsvariablen (Aspire-Parameter).
// Remarks: https://learn.microsoft.com/dotnet/aspire/authentication/keycloak-integration#add-openid-connect-authentication
//          https://learn.microsoft.com/aspnet/core/blazor/security/blazor-web-app-with-oidc?pivots=with-yarp-and-aspire
// ---------------------------------------------------------------------------------------------
var keycloak = builder.Configuration.GetSection(KeycloakOptions.SectionName).Get<KeycloakOptions>() ?? new KeycloakOptions();
builder.Services.AddAuthentication(OidcScheme)
    .AddKeycloakOpenIdConnect(keycloak.ServiceName, keycloak.Realm, OidcScheme, options =>
    {
        options.ClientId = keycloak.ClientId;
        options.ClientSecret = keycloak.ClientSecret;
        options.RequireHttpsMetadata = keycloak.RequireHttpsMetadata;

        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.Scope.Add(OpenIdConnectScope.OpenIdProfile);
        options.MapInboundClaims = false;
        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.RoleClaimType = "roles";
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "__Host-blueprint";
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

// Access Token automatisch per Refresh Token erneuern (aus dem Microsoft-Sample "BlazorWebAppOidcBff").
builder.Services.ConfigureCookieOidc(CookieAuthenticationDefaults.AuthenticationScheme, OidcScheme);
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Blazor: Server-Prerendering + WebAssembly-Interaktivität; Auth-Status wird an den Client serialisiert.
// Remarks: https://learn.microsoft.com/aspnet/core/blazor/components/render-modes
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

builder.Services.AddFluentUIComponents();

// API-Clients für das Prerendering auf dem Server (gleiche Klassen wie im Browser, aber direkt gegen die API + Token).
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ServerAccessTokenHandler>();
builder.Services.AddBlueprintApiClients(new Uri(apiBaseAddress), client => client.AddHttpMessageHandler<ServerAccessTokenHandler>());

// YARP-Forwarder mit Service Discovery
// Remarks: https://learn.microsoft.com/aspnet/core/fundamentals/servers/yarp/direct-forwarding
builder.Services.AddHttpForwarderWithServiceDiscovery();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Blueprint.Web.Client._Imports).Assembly);

// BFF: /api/** → Web API, mit dem Access Token aus der Cookie-Session.
app.MapForwarder("/api/{**catch-all}", apiBaseAddress, transformBuilder =>
{
    transformBuilder.AddRequestTransform(async transformContext =>
    {
        var accessToken = await transformContext.HttpContext.GetTokenAsync("access_token");
        transformContext.ProxyRequest.Headers.Authorization = new("Bearer", accessToken);
    });
}).RequireAuthorization();

app.MapGroup("/authentication").MapLoginAndLogout(OidcScheme);
app.MapDefaultEndpoints();

await app.RunAsync();
