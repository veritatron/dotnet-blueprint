using Blueprint.Web.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Fluent UI Blazor – Registrierung in Server- UND Client-Projekt (WebAssembly-Interaktivität)
// Remarks: https://www.fluentui-blazor.net
builder.Services.AddFluentUIComponents();

// Authentifizierungsstatus wird vom Server (BFF) beim Prerendering übergeben – keine Tokens im Browser.
// Remarks: https://learn.microsoft.com/aspnet/core/blazor/security/#manage-authentication-state-in-blazor-web-apps
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();

// Generierte Refit-Clients (OpenAPI → Refitter); Aufrufe gehen relativ an den BFF (gleiche Origin, Cookie).
// Remarks: https://learn.microsoft.com/aspnet/core/blazor/call-web-api
builder.Services.AddBlueprintApiClients(new Uri(builder.HostEnvironment.BaseAddress));

await builder.Build().RunAsync();
