# dotnet-blueprint – Modularer Monolith mit .NET 10

Blueprint für einen **modularen Monolithen** mit **ASP.NET Core Web API**, **Blazor WebAssembly** (Fluent UI Blazor v5)
und einem **Backend for Frontend (BFF)**. Ziel: heute einfach als *eine* Anwendung betreiben, später einzelne Module
mit minimalem Aufwand als **Microservices** auslagern.

| Anforderung | Umsetzung |
|---|---|
| Modularer Monolith, später Microservices | Ein Modul = eigene Projekte (Domain/Application/Infrastructure/Contracts), eigener `DbContext`, eigenes DB-Schema, Kommunikation nur über `*.Contracts`. Grenzen werden durch Architekturtests erzwungen. |
| Gemeinsame Oracle-DB am Anfang | Ein Connection String `blueprintdb`, aber **ein Schema pro Modul** (`CATALOG`, `ORDERING`). Pro Modul kann ein eigener Connection String gesetzt werden. |
| Später PostgreSQL | Provider ist Konfiguration (`Database:Provider`). Migrations existieren für **beide** Provider. |
| Integration Events: erst In-Memory, später Kafka/RabbitMQ/… | `IEventBus`-Abstraktion + **Transactional Outbox** + **Inbox** (Idempotenz). Transport per `Messaging:Transport`. |
| Module/Services müssen Daten voneinander abfragen | Synchrone **Modul-APIs** (`ICatalogModuleApi`) in den Contracts: in-process im Monolith, HTTP-Client (`Blueprint.Modules.Catalog.Client`) nach Auslagerung. |
| .NET 10, Fluent UI Blazor, BFF | Blazor Web App (WASM-Interaktivität) als BFF mit OIDC-Cookie + YARP-Forwarding – Muster aus der Microsoft-Doku. |
| Nur Microsoft-Best-Practices | Jede relevante Stelle verweist per `<remarks>` auf die Microsoft-Dokumentation. Keine Fremd-Frameworks für Mediator/Mapping/Validierung. |

![Produkte](docs/images/products.png)

## Architektur

```mermaid
flowchart LR
    Browser["Browser<br/>Blazor WASM (Fluent UI)"] -- "Cookie<br/>/api/**" --> BFF
    subgraph BFF["Blueprint.Web (BFF)"]
        OIDC["OIDC Login<br/>Cookie-Session"]
        YARP["YARP Forwarder<br/>+ Access Token"]
    end
    BFF -- "OIDC Code + PKCE" --> IdP[(Keycloak / Entra ID)]
    YARP -- "Bearer JWT" --> API
    subgraph API["Blueprint.Api (Modular Monolith)"]
        Catalog["Catalog-Modul"]
        Ordering["Ordering-Modul"]
        Bus{{"IEventBus<br/>(InMemory → Broker)"}}
        Ordering -- "ICatalogModuleApi (sync)" --> Catalog
        Catalog -- Outbox --> Bus
        Ordering -- Outbox --> Bus
        Bus -- Inbox --> Catalog
        Bus -- Inbox --> Ordering
    end
    Catalog --> DB[("Oracle / PostgreSQL<br/>Schema CATALOG")]
    Ordering --> DB2[("Oracle / PostgreSQL<br/>Schema ORDERING")]
```

### Beispiel-Ablauf (Choreografie-Saga)

1. `POST /api/ordering/orders` – Ordering holt Produktname/Preis **synchron** über `ICatalogModuleApi`.
2. Die Bestellung wird als `Pending` gespeichert; das Domain Event `OrderPlaced` wird in derselben Transaktion als
   Integration Event in die **Outbox** geschrieben → Antwort `202 Accepted`.
3. Der Outbox-Processor veröffentlicht `OrderPlacedIntegrationEvent` über den Event Bus.
4. Catalog reserviert den Bestand (alles-oder-nichts) und antwortet mit `StockReserved` bzw. `StockReservationFailed`.
5. Ordering setzt die Bestellung auf `Confirmed` bzw. `Rejected`. Jeder Handler läuft über die **Inbox** (idempotent).

### Projektstruktur

```
src/
  Aspire/
    Blueprint.AppHost            Lokale Orchestrierung: Oracle|PostgreSQL, Keycloak, API, BFF (+ Dashboard)
    Blueprint.ServiceDefaults    OpenTelemetry, Health Checks, Service Discovery, Resilience
  BuildingBlocks/
    *.Domain                     Entity, AggregateRoot, Domain Events, Result/Error
    *.Contracts                  IntegrationEvent (ohne Abhängigkeiten → NuGet-fähig)
    *.Application                Command/Query/Event-Handler-Abstraktionen, Unit of Work, Outbox-Interface
    *.Infrastructure             ModuleDbContext, Provider-Umschaltung, Outbox/Inbox, Event Bus, Problem Details
  Modules/
    Catalog/  Domain | Application | Infrastructure | Contracts | Client
    Ordering/ Domain | Application | Infrastructure | Contracts
  Hosts/
    Blueprint.Api                Modulith-Host (setzt die Module zusammen)
    Blueprint.Web                BFF + Blazor-Server-Host (Prerendering)
    Blueprint.Web.Client         Blazor WebAssembly
tests/
  Blueprint.ArchitectureTests    Modulgrenzen & Schichtenregeln
  Blueprint.UnitTests            Domain- und Handler-Tests (MSTest)
docs/                            Vertiefungen (Messaging, Datenbank, Frontend, Auslagerung)
```

**Abhängigkeitsregeln** (geprüft in `tests/Blueprint.ArchitectureTests`):

- `Domain` kennt weder EF Core noch ASP.NET Core.
- `Application` kennt keine Infrastruktur.
- Ein Modul darf von anderen Modulen **nur** `*.Contracts` referenzieren.
- `*.Contracts` hängen nur von `Blueprint.BuildingBlocks.Contracts` ab.
- Der WebAssembly-Client kennt nur `*.Contracts`.

## Schnellstart

Voraussetzungen: .NET 10 SDK, Docker bzw. Podman (für Aspire), vertrauenswürdiges HTTPS-Entwicklerzertifikat
(`dotnet dev-certs https --trust`, unter Linux siehe [docs/frontend.md](docs/frontend.md#entwicklung-unter-linux)).

```bash
dotnet tool restore
dotnet run --project src/Aspire/Blueprint.AppHost
```

Das Aspire-Dashboard öffnet sich; die Anwendung läuft unter `https://localhost:7201`.
Testbenutzer (Keycloak-Realm `blueprint`, nur für die Entwicklung):

| Benutzer | Passwort | Rollen |
|---|---|---|
| `alice` | `alice` | `catalog-admin` (darf Produkte anlegen) |
| `bob` | `bob` | – |

PostgreSQL statt Oracle:

```bash
dotnet run --project src/Aspire/Blueprint.AppHost -- --Database:Provider=PostgreSql
```

### API ohne Identity Provider testen

```bash
# Erstellt ein lokales Entwickler-Token und konfiguriert die API dafür (appsettings.Development.json + User Secrets)
dotnet user-jwts create --project src/Hosts/Blueprint.Api --audience blueprint-api --claim roles=catalog-admin
```

Anschließend `src/Hosts/Blueprint.Api/Blueprint.Api.http` verwenden.
Siehe [Manage JSON Web Tokens in development with dotnet user-jwts](https://learn.microsoft.com/aspnet/core/security/authentication/jwt-authn).

### Tests

```bash
dotnet test dotnet-blueprint.slnx
```

## Konfiguration

| Schlüssel | Standard | Bedeutung |
|---|---|---|
| `Database:Provider` | `Oracle` | `Oracle` oder `PostgreSql` |
| `ConnectionStrings:blueprintdb` | – | Gemeinsamer Connection String aller Module |
| `ConnectionStrings:<modul>` | – | Optional: eigene Datenbank für ein Modul (z. B. `catalog`) |
| `Database:OracleSqlCompatibility` | `DatabaseVersion23` | Oracle-SQL-Dialekt (`DatabaseVersion19`, `…21`, `…23`) |
| `Messaging:Transport` | `InMemory` | Event-Bus-Transport |
| `Messaging:Outbox:*` | 2 s / 50 / 10 | Polling-Intervall, Batchgröße, max. Versuche |
| `Authentication:Schemes:Bearer:*` | – | JWT-Validierung der API (Authority, ValidAudiences …) |
| `Authentication:Oidc:*` | – | OIDC-Client des BFF (Authority, ClientId, ClientSecret) |

## Weiterführende Dokumentation

- [docs/messaging.md](docs/messaging.md) – Domain vs. Integration Events, Outbox/Inbox, Broker anbinden (RabbitMQ, Kafka, Azure Service Bus)
- [docs/database.md](docs/database.md) – Schemas pro Modul, Oracle ↔ PostgreSQL, Migrations
- [docs/frontend.md](docs/frontend.md) – BFF, Authentifizierung, Fluent UI Blazor
- [docs/microservice-extraction.md](docs/microservice-extraction.md) – Schritt für Schritt ein Modul auslagern

## Microsoft-Referenzen (Auswahl)

- [.NET Microservices: Architecture for Containerized .NET Applications](https://learn.microsoft.com/dotnet/architecture/microservices/)
- [Common web application architectures (Clean Architecture)](https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)
- [Migrate a monolith to microservices (Strangler Fig)](https://learn.microsoft.com/azure/architecture/microservices/migrate-monolith)
- [Secure a Blazor Web App with OIDC and the BFF pattern](https://learn.microsoft.com/aspnet/core/blazor/security/blazor-web-app-with-oidc?pivots=with-yarp-and-aspire)
- [Aspire overview](https://learn.microsoft.com/dotnet/aspire/get-started/aspire-overview)
- [EF Core – Migrations with multiple providers](https://learn.microsoft.com/ef/core/managing-schemas/migrations/providers)
- [Transactional Outbox pattern](https://learn.microsoft.com/azure/architecture/databases/guide/transactional-outbox-cosmos)
