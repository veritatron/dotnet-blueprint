using Blueprint.BuildingBlocks.Infrastructure.Messaging;
using Blueprint.BuildingBlocks.Infrastructure.Persistence;
using Blueprint.BuildingBlocks.Infrastructure.Web;
using Blueprint.Modules.Catalog.Infrastructure;
using Blueprint.Modules.Ordering.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Querschnitt: OpenTelemetry, Health Checks, Service Discovery, Resilience
// Remarks: https://learn.microsoft.com/dotnet/aspire/fundamentals/service-defaults
builder.AddServiceDefaults();

// Fehlerbehandlung nach RFC 9457 (Problem Details)
// Remarks: https://learn.microsoft.com/aspnet/core/fundamentals/error-handling-api
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// OpenAPI-Dokument (eingebaut seit .NET 9)
// Remarks: https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi
builder.Services.AddOpenApi();

// Authentifizierung: JWT Bearer. Konfiguration über "Authentication:Schemes:Bearer" (Authority, ValidAudiences, …).
// Lokal ohne Identity Provider: `dotnet user-jwts create` (siehe README).
// Remarks: https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication
//          https://learn.microsoft.com/aspnet/core/security/authentication/jwt-authn
builder.Services.AddAuthentication().AddJwtBearer(options =>
{
    // Original-Claim-Namen des Tokens behalten (sub, roles, …) statt auf WS-Federation-URIs zu mappen.
    options.MapInboundClaims = false;
    options.TokenValidationParameters.NameClaimType = "name";
    options.TokenValidationParameters.RoleClaimType = "roles";
});
builder.Services.AddAuthorization();

// Event Bus (Transport per Konfiguration "Messaging:Transport", Start: InMemory)
builder.AddEventBus();

// Module – Reihenfolge egal, Module kennen sich nur über ihre Contracts.
builder.AddCatalogModule();
builder.AddOrderingModule();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Nur Development! Produktion: idempotente SQL-Skripte / Migration Bundles im Deployment.
    // Remarks: https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying
    await app.MigrateModuleDatabasesAsync();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapCatalogModule();
app.MapOrderingModule();
app.MapDefaultEndpoints();

await app.RunAsync();

/// <summary>Für Integrationstests mit WebApplicationFactory.</summary>
/// <remarks>https://learn.microsoft.com/aspnet/core/test/integration-tests</remarks>
public partial class Program;
