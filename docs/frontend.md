# Frontend: Blazor WebAssembly, BFF und Fluent UI Blazor

## Backend for Frontend (BFF)

`Blueprint.Web` ist eine Blazor Web App mit **globaler WebAssembly-Interaktivität** und übernimmt die Rolle des BFF:

1. **Login** per OIDC Authorization Code Flow + PKCE (und PAR, falls vom IdP unterstützt) – serverseitig, vertrauliches Client-Secret.
2. **Session** als verschlüsseltes, `HttpOnly`-, `Secure`-, `SameSite=Lax`-Cookie. **Tokens gelangen nie in den Browser.**
3. **Token-Refresh** automatisch per Refresh Token (`CookieOidcRefresher` aus dem Microsoft-Sample).
4. **API-Aufrufe** des WASM-Clients gehen relativ an `/api/**`; YARP leitet sie mit dem Access Token an `Blueprint.Api` weiter.
5. **Prerendering**: Beim serverseitigen Prerendering rufen dieselben Typed Clients die API direkt auf
   (`ServerAccessTokenHandler`); per `[PersistentState]` werden die Daten an den Client übergeben (kein doppelter Abruf).

Remarks:
- [Secure an ASP.NET Core Blazor Web App with OIDC (BFF mit YARP und Aspire)](https://learn.microsoft.com/aspnet/core/blazor/security/blazor-web-app-with-oidc?pivots=with-yarp-and-aspire)
- [Blazor render modes](https://learn.microsoft.com/aspnet/core/blazor/components/render-modes)
- [Prerendered state persistence](https://learn.microsoft.com/aspnet/core/blazor/state-management/prerendered-state-persistence)
- [YARP direct forwarding](https://learn.microsoft.com/aspnet/core/fundamentals/servers/yarp/direct-forwarding)

## Authentifizierung mit Keycloak (OIDC)

Beide Hosts verwenden die **Aspire-Keycloak-Integration** (`Aspire.Keycloak.Authentication`). Die Keycloak-Adresse
kommt per Service Discovery (`keycloak`), Realm/Client/Audience aus `Authentication:Keycloak`:

| Host | Aufruf | Zweck |
|---|---|---|
| `Blueprint.Web` (BFF) | `AddKeycloakOpenIdConnect("keycloak", realm, …)` | Login (Code + PKCE, PAR), Cookie-Session, Refresh Token |
| `Blueprint.Api` | `AddKeycloakJwtBearer("keycloak", realm, …)` | Validierung der Access Tokens (Issuer, Signatur, Audience `blueprint-api`) |

Realm `blueprint` (Import aus `src/Aspire/Blueprint.AppHost/Realms/blueprint-realm.json`):

- Client `blueprint-web` – vertraulich, Authorization Code + PKCE (S256), Mapper für Audience `blueprint-api` und
  Realm-Rollen als Claim `roles`.
- Client `blueprint-dev` – nur Entwicklung: Client Credentials (Service Account mit `catalog-admin`) für `.http`-Dateien
  und als Vorlage für Service-zu-Service-Aufrufe.
- Rolle `catalog-admin`, Benutzer `alice` (Admin) und `bob`.

Mit vertrauenswürdigem Entwicklerzertifikat startet Aspire Keycloak per **HTTPS** (`https://localhost:8080`).
Ohne Aspire: `Services:keycloak:https:0` bzw. `…:http:0` setzen (siehe `appsettings.Development.json`).

Remarks:
- [Aspire Keycloak integration](https://learn.microsoft.com/dotnet/aspire/authentication/keycloak-integration)
- [Configure JWT bearer authentication](https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication)
- [Keycloak: Server Administration – OIDC clients](https://www.keycloak.org/docs/latest/server_admin/#_oidc_clients)

## API-Clients

Blazor-Komponenten injizieren die **generierten** Refit-Interfaces (`ICatalogApi`, `IOrderingApi`), siehe
[api-clients.md](api-clients.md). Registrierung: `AddBlueprintApiClients(baseAddress, …)` – im Browser mit der
BFF-Adresse, beim Prerendering mit der API-Adresse plus `ServerAccessTokenHandler`.

## Fluent UI Blazor v5

- Registrierung (`AddFluentUIComponents`) in **Server- und Client-Projekt**, `<FluentProviders />` im `MainLayout`.
- `FluentNumberInput` lädt die Bibliothek **IMask** standardmäßig zur Laufzeit von `unpkg.com`. Der Blueprint liefert sie
  stattdessen lokal aus (`Blueprint.Web/wwwroot/lib/imask`, MIT-Lizenz) – kein CDN zur Laufzeit, CSP-freundlich, offline-fähig.
  Bei Updates von Fluent UI die erwartete IMask-Version im Paket prüfen (`Microsoft.FluentUI.AspNetCore.Components.lib.module.js`).
- Formulare binden an die generierten Request-Modelle; deren DataAnnotations stammen aus dem OpenAPI-Schema und
  entsprechen damit der serverseitigen Minimal-API-Validierung von .NET 10.

Remarks: [Fluent UI Blazor](https://www.fluentui-blazor.net), [Blazor forms validation](https://learn.microsoft.com/aspnet/core/blazor/forms/validation)

## Entwicklung unter Linux

Das ASP.NET-Core-Entwicklerzertifikat muss vertrauenswürdig sein, damit der BFF die API per HTTPS aufrufen kann:

```bash
dotnet dev-certs https --trust
export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/usr/lib/ssl/certs"
```

Remarks: [Trust the ASP.NET Core HTTPS development certificate](https://learn.microsoft.com/aspnet/core/security/enforcing-ssl#trust-the-aspnet-core-https-development-certificate)
