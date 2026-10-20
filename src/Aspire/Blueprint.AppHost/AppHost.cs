var builder = DistributedApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------------------------
// Datenbank: Oracle (Standard) oder PostgreSQL – umschaltbar über "Database:Provider" (appsettings / --Database:Provider=PostgreSql).
// Remarks: https://learn.microsoft.com/dotnet/aspire/database/oracle-entity-framework-integration
//          https://learn.microsoft.com/dotnet/aspire/database/postgresql-entity-framework-integration
// ---------------------------------------------------------------------------------------------
var databaseProvider = builder.Configuration["Database:Provider"] ?? "Oracle";

IResourceBuilder<IResourceWithConnectionString> database = databaseProvider switch
{
    "PostgreSql" => builder.AddPostgres("postgres")
        .WithDataVolume()
        .WithLifetime(ContainerLifetime.Persistent)
        .AddDatabase("blueprintdb"),

    // Oracle: ein Schema (= Benutzer) pro Modul, angelegt durch ./OracleInit beim ersten Start des Containers.
    _ => builder.AddOracle("oracle")
        .WithDataVolume()
        .WithLifetime(ContainerLifetime.Persistent)
        .WithInitFiles("./OracleInit")
        .AddDatabase("blueprintdb", "FREEPDB1"),
};

// ---------------------------------------------------------------------------------------------
// Identity Provider für die lokale Entwicklung (OIDC). In Produktion z. B. Microsoft Entra ID – nur Konfiguration.
// Remarks: https://learn.microsoft.com/dotnet/aspire/authentication/keycloak-integration
// ---------------------------------------------------------------------------------------------
var keycloak = builder.AddKeycloak("keycloak", port: 8080)
    .WithDataVolume()
    .WithRealmImport("./Realms")
    .WithLifetime(ContainerLifetime.Persistent);

var realmUrl = ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/blueprint");
var webClientSecret = builder.AddParameter("web-client-secret", secret: true);

var api = builder.AddProject<Projects.Blueprint_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WaitFor(keycloak)
    .WithEnvironment("Database__Provider", databaseProvider)
    .WithEnvironment("Authentication__Schemes__Bearer__Authority", realmUrl)
    .WithEnvironment("Authentication__Schemes__Bearer__RequireHttpsMetadata", "false");

builder.AddProject<Projects.Blueprint_Web>("web")
    .WithExternalHttpEndpoints()
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("Authentication__Oidc__Authority", realmUrl)
    .WithEnvironment("Authentication__Oidc__ClientSecret", webClientSecret);

await builder.Build().RunAsync();
