# API-Clients: OpenAPI → Refitter → Refit

Die HTTP-Clients werden **nicht von Hand geschrieben**, sondern aus den OpenAPI-Dokumenten der Module generiert.

```
Minimal-API-Endpunkte (Modul)                    ─┐
  .WithGroupName("catalog"), DataAnnotations      │  dotnet build (Blueprint.Api)
  AddModuleOpenApi("catalog", …)                  │  Microsoft.Extensions.ApiDescription.Server
                                                  ▼
openapi/blueprint_catalog.json   (eingecheckt, OpenAPI 3.0)
                                                  │  dotnet build (Blueprint.Modules.Catalog.Client)
                                                  │  Refitter.MSBuild + catalog.refitter
                                                  ▼
Generated/CatalogApi.g.cs        (eingecheckt)  →  ICatalogApi (Refit) + Models (inkl. DataAnnotations)
                                                  │  Refit Source Generator
                                                  ▼
AddRefitGeneratedClient<ICatalogApi>()             reflection-freie Implementierung (WebAssembly/Trimming-tauglich)
```

## Bausteine

| Baustein | Zweck | Referenz |
|---|---|---|
| `AddModuleOpenApi(module, title)` (BuildingBlocks) | ein Dokument pro Modul, OpenAPI 3.0, Bearer-Security, `decimal`-Format, strikte Zahlen | [ASP.NET Core OpenAPI](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi) |
| `Microsoft.Extensions.ApiDescription.Server` (Blueprint.Api) | schreibt die Dokumente beim Build nach `openapi/` | [Build-time generation](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi#generate-openapi-documents-at-build-time) |
| `*.refitter` + `Refitter.MSBuild` (`*.Client`) | generiert Refit-Interface + DTOs bei jedem Build | [Refitter](https://refitter.github.io/) |
| `Refit.HttpClientFactory` | Registrierung über `IHttpClientFactory` | [IHttpClientFactory](https://learn.microsoft.com/dotnet/core/extensions/httpclient-factory) |

Projekte:

- `Blueprint.Modules.Catalog.Client` – `ICatalogApi`, `AddCatalogApiClient(...)`, `AddCatalogModuleHttpClient(...)`
  (HTTP-Implementierung von `ICatalogModuleApi` für ausgelagerte Szenarien)
- `Blueprint.Modules.Ordering.Client` – `IOrderingApi`, `AddOrderingApiClient(...)`

Der Blazor-WebAssembly-Client referenziert **nur** diese generierten Projekte (Architekturtest).

## Workflow bei API-Änderungen

1. Endpunkt/DTO im Modul ändern.
2. `dotnet build` (Solution) – die API schreibt `openapi/*.json`, danach erzeugt Refitter die Clients neu
   (die Build-Reihenfolge ist in `dotnet-blueprint.slnx` per `BuildDependency` festgelegt).
3. Diff von `openapi/` und `Generated/` prüfen (Breaking Changes sind hier sofort sichtbar) und mitcommitten.
   Die CI schlägt fehl, wenn der Build Änderungen erzeugt, die nicht eingecheckt sind.

Nur Clients neu generieren: `dotnet build src/Modules/Catalog/Blueprint.Modules.Catalog.Client -t:RefitterGenerate`.

## Konventionen für einen guten Vertrag

- Jeder Endpunkt hat einen stabilen `WithName(...)` → `operationId` → Methodenname (`GetProductsAsync`).
- `TypedResults` + `ProducesProblem`/`ProducesValidationProblem` dokumentieren alle Antworten
  ([Include OpenAPI metadata](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/include-metadata)).
- DataAnnotations an Request-DTOs (`[Required]`, `[StringLength(…, MinimumLength = 1)]`, `[Range]`) landen im Schema
  und damit als Attribute in den generierten Modellen → gleiche Validierung in Blazor-Formularen und API.
- Fehler kommen als Problem Details (RFC 9457); im Client liest `ApiErrors.GetMessageAsync(ApiException)` sie aus.

## Nach einer Auslagerung

Ein ausgelagerter Service liefert dasselbe Dokument (`AddCatalogModule` registriert es). In den `.refitter`-Dateien
kann `openApiPath` dann auch direkt auf die URL des Service bzw. ein veröffentlichtes Artefakt zeigen.
