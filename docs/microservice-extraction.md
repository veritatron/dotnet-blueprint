# Ein Modul als Microservice auslagern

Beispiel: **Catalog** wird ein eigener Service, Ordering bleibt im Modulith. Vorgehen nach dem
[Strangler-Fig-Muster](https://learn.microsoft.com/azure/architecture/patterns/strangler-fig) – schrittweise, jederzeit lauffähig.

## 1. Eigene Datenbank (optional, aber empfohlen)

`ConnectionStrings:catalog` auf eine eigene Datenbank zeigen lassen und das Schema `CATALOG` dorthin migrieren.
Da es keine modulübergreifenden Fremdschlüssel gibt, ist das ein reiner Datenumzug.

## 2. Event Bus auf einen Broker umstellen

Solange beide Teile in einem Prozess laufen, reicht `InMemory`. Für getrennte Prozesse wird ein Broker benötigt
(siehe [messaging.md](messaging.md)): `Messaging:Transport=RabbitMQ` (oder Kafka/Azure Service Bus) in **beiden** Hosts.

## 3. Neuen Host anlegen

```bash
dotnet new web -n Blueprint.Services.Catalog -o src/Services/Blueprint.Services.Catalog
```

`Program.cs` entspricht `Blueprint.Api`, registriert aber **nur** das Catalog-Modul:

```csharp
builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddAuthentication().AddJwtBearer(/* wie Blueprint.Api */);
builder.AddEventBus();
builder.AddCatalogModule();
// …
app.MapCatalogModule();
```

Integration-Event-Handler (z. B. `OrderPlacedIntegrationEventHandler`) wandern automatisch mit, weil das Modul sie selbst registriert.

## 4. Modulith umstellen

In `Blueprint.Api`:

```csharp
// builder.AddCatalogModule();   ← entfernen
// Paket "Microsoft.AspNetCore.HeaderPropagation" hinzufügen
builder.Services.AddCatalogModuleHttpClient(new Uri("https+http://catalog"))   // ICatalogModuleApi per HTTP
    .AddHeaderPropagation();                                                     // Benutzer-Token weiterreichen
builder.Services.AddHeaderPropagation(o => o.Headers.Add("Authorization"));
// …
app.UseHeaderPropagation();
```

`PlaceOrderCommandHandler` und alle anderen Aufrufer bleiben **unverändert** – sie kennen nur `ICatalogModuleApi`.
Für Service-zu-Service-Aufrufe ohne Benutzerkontext stattdessen Client-Credentials verwenden.

Remarks: [Header propagation](https://learn.microsoft.com/aspnet/core/fundamentals/http-requests#header-propagation-middleware),
[HTTP resilience](https://learn.microsoft.com/dotnet/core/resilience/http-resilience),
[Service discovery](https://learn.microsoft.com/dotnet/core/extensions/service-discovery)

## 5. Aspire und BFF

```csharp
var catalog = builder.AddProject<Projects.Blueprint_Services_Catalog>("catalog").WithReference(catalogDb).WithReference(messaging);
var api = builder.AddProject<Projects.Blueprint_Api>("api").WithReference(catalog) /* … */;
```

Der BFF leitet `/api/catalog/**` an `https+http://catalog` und alles andere weiterhin an `https+http://api` weiter
(zwei `MapForwarder`-Aufrufe). Der Blazor-Client ändert sich nicht, weil die Routen in `CatalogRoutes` gleich bleiben.

## 6. Contracts als Paket

`Blueprint.BuildingBlocks.Contracts`, `Blueprint.Modules.Catalog.Contracts` und `Blueprint.Modules.Catalog.Client`
als NuGet-Pakete veröffentlichen, sobald die Services in getrennten Repositories leben
([Create a NuGet package](https://learn.microsoft.com/nuget/create-packages/creating-a-package-dotnet-cli)).
Integration Events dann versioniert weiterentwickeln (nur additive Änderungen).

## Checkliste

- [ ] Architekturtests grün (keine Referenzen auf Interna anderer Module)
- [ ] Eigene Datenbank/Schema, eigener Connection String
- [ ] Broker statt In-Memory in allen beteiligten Hosts
- [ ] `ICatalogModuleApi` im Modulith per HTTP-Client registriert
- [ ] BFF-Routing angepasst
- [ ] Health Checks, Telemetrie (ServiceDefaults) im neuen Service
