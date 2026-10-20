# Datenbank: ein Schema pro Modul, Oracle ↔ PostgreSQL

## Prinzip

- Jedes Modul hat einen eigenen `DbContext` (abgeleitet von `ModuleDbContext`) und ein **eigenes Schema**
  (`CATALOG`/`ORDERING` auf Oracle, `catalog`/`ordering` auf PostgreSQL) inkl. eigener `__EFMigrationsHistory`, Outbox und Inbox.
- Es gibt **keine Fremdschlüssel und keine Joins über Modulgrenzen**. Daten anderer Module werden über die Modul-API
  abgefragt oder per Integration Event repliziert (z. B. kopiert die Bestellung Produktname und Preis).
- Dadurch kann ein Modul jederzeit in eine eigene Datenbank umziehen: `ConnectionStrings:<modul>` setzen – fertig.

Remarks: [Data sovereignty per microservice](https://learn.microsoft.com/dotnet/architecture/microservices/architect-microservice-container-applications/data-sovereignty-per-microservice),
[Distributed data management](https://learn.microsoft.com/dotnet/architecture/microservices/architect-microservice-container-applications/distributed-data-management)

## Provider wechseln

```json
"Database": { "Provider": "PostgreSql" },
"ConnectionStrings": { "blueprintdb": "Host=…;Database=blueprint;Username=…;Password=…" }
```

Provider-neutral gehalten:
- Domain und Application kennen keinen Provider.
- Optimistische Nebenläufigkeit über eine app-verwaltete `Version`-Spalte (statt `rowversion`/`xmin`) –
  [Application-managed concurrency tokens](https://learn.microsoft.com/ef/core/saving/concurrency#application-managed-concurrency-tokens).
- Provider-Besonderheiten nur in der Infrastruktur (z. B. `NCLOB` für Outbox-Payloads auf Oracle, weil Oracle unbegrenzte Strings sonst als `NVARCHAR2(2000)` anlegt).
- Kein `bool` in Tabellen der Building Blocks (Oracle < 23ai kennt kein `BOOLEAN`).

## Migrations

Nach dem Microsoft-Muster [„Using multiple context types“](https://learn.microsoft.com/ef/core/managing-schemas/migrations/providers#using-multiple-context-types)
gibt es pro Modul zwei abgeleitete Kontexte (`Oracle…DbContext`, `PostgreSql…DbContext`) mit eigenen Migrations-Ordnern.
Design-Time-Factories erlauben `dotnet ef` ohne laufende Datenbank.

Neue Migration (für **beide** Provider):

```bash
dotnet tool restore
M=Catalog; NAME=AddProductCategory
for P in Oracle PostgreSql; do
  dotnet ef migrations add $NAME \
    --project src/Modules/$M/Blueprint.Modules.$M.Infrastructure \
    --context ${P}${M}DbContext --output-dir Persistence/Migrations/$P
done
```

Die CI prüft mit `dotnet ef migrations has-pending-model-changes`, dass keine Migration vergessen wurde.

### Anwenden

- **Entwicklung:** `Blueprint.Api` wendet die Migrations aller Module beim Start an (nur `Development`).
- **Produktion:** idempotente Skripte oder Migration Bundles im Deployment-Prozess
  ([Applying migrations](https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying)):

```bash
dotnet ef migrations script --idempotent --project src/Modules/Catalog/Blueprint.Modules.Catalog.Infrastructure --context OracleCatalogDbContext -o catalog-oracle.sql
dotnet ef migrations bundle --project src/Modules/Catalog/Blueprint.Modules.Catalog.Infrastructure --context OracleCatalogDbContext
```

## Oracle-Besonderheiten

- In Oracle ist ein Schema ein Benutzer. EF Core legt diese **nicht** an. Lokal erledigt das
  `src/Aspire/Blueprint.AppHost/OracleInit/01-create-module-schemas.sql` beim ersten Start des Containers.
  In anderen Umgebungen muss der DBA die Schemas (`CREATE USER CATALOG NO AUTHENTICATION …`) anlegen
  und dem Anwendungsbenutzer die nötigen Rechte geben.
- Der Anwendungsbenutzer benötigt für Migrations Rechte, in den Modul-Schemas Objekte anzulegen
  (Produktion: Migrations mit einem separaten, privilegierten Deployment-Benutzer ausführen).
- `Database:OracleSqlCompatibility` an die Zielversion anpassen (`DatabaseVersion19` für 19c).

Remarks: [Oracle EF Core provider](https://docs.oracle.com/en/database/oracle/oracle-database/23/odpnt/ODPEFCore.html),
[Aspire Oracle integration](https://learn.microsoft.com/dotnet/aspire/database/oracle-entity-framework-integration)
