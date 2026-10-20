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

## Identity Provider

Lokal: Keycloak (Aspire, Realm `src/Aspire/Blueprint.AppHost/Realms/blueprint-realm.json`). Der Client `blueprint-web`
erhält per Mapper die Audience `blueprint-api` und Realm-Rollen als Claim `roles`.
Für Microsoft Entra ID nur Konfiguration ändern (`Authentication:Oidc:*` im BFF, `Authentication:Schemes:Bearer:*` in der API),
App-Rollen erscheinen dort ebenfalls als `roles`-Claim.

Remarks: [Microsoft identity platform – app roles](https://learn.microsoft.com/entra/identity-platform/howto-add-app-roles-in-apps)

## Fluent UI Blazor v5

- Registrierung (`AddFluentUIComponents`) in **Server- und Client-Projekt**, `<FluentProviders />` im `MainLayout`.
- `FluentNumberInput` lädt die Bibliothek **IMask** standardmäßig zur Laufzeit von `unpkg.com`. Der Blueprint liefert sie
  stattdessen lokal aus (`Blueprint.Web/wwwroot/lib/imask`, MIT-Lizenz) – kein CDN zur Laufzeit, CSP-freundlich, offline-fähig.
  Bei Updates von Fluent UI die erwartete IMask-Version im Paket prüfen (`Microsoft.FluentUI.AspNetCore.Components.lib.module.js`).
- Formulare validieren mit denselben DataAnnotations wie die API (`*.Contracts`), serverseitig über die eingebaute
  Minimal-API-Validierung von .NET 10.

Remarks: [Fluent UI Blazor](https://www.fluentui-blazor.net), [Blazor forms validation](https://learn.microsoft.com/aspnet/core/blazor/forms/validation)

## Entwicklung unter Linux

Das ASP.NET-Core-Entwicklerzertifikat muss vertrauenswürdig sein, damit der BFF die API per HTTPS aufrufen kann:

```bash
dotnet dev-certs https --trust
export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/usr/lib/ssl/certs"
```

Remarks: [Trust the ASP.NET Core HTTPS development certificate](https://learn.microsoft.com/aspnet/core/security/enforcing-ssl#trust-the-aspnet-core-https-development-certificate)
