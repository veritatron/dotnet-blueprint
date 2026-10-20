namespace Blueprint.ServiceDefaults.Authentication;

/// <summary>
/// Keycloak-Einstellungen (Abschnitt <c>Authentication:Keycloak</c>), gemeinsam für API (JWT Bearer) und BFF (OIDC).
/// Die Server-Adresse wird NICHT hier konfiguriert, sondern per Service Discovery über <see cref="ServiceName"/>
/// aufgelöst (Aspire: <c>WithReference(keycloak)</c>, sonst <c>Services:keycloak:http:0</c>).
/// </summary>
/// <remarks>
/// https://learn.microsoft.com/dotnet/aspire/authentication/keycloak-integration
/// </remarks>
public sealed class KeycloakOptions
{
    public const string SectionName = "Authentication:Keycloak";

    public string ServiceName { get; set; } = "keycloak";

    public string Realm { get; set; } = "blueprint";

    /// <summary>Audience der API (Access Tokens müssen sie im <c>aud</c>-Claim enthalten).</summary>
    public string Audience { get; set; } = "blueprint-api";

    /// <summary>OIDC-Client des BFF.</summary>
    public string ClientId { get; set; } = "blueprint-web";

    /// <summary>Client-Secret des BFF – nur über User Secrets / Umgebungsvariablen / Key Vault setzen.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>Nur lokal mit Keycloak über HTTP auf <c>false</c> setzen.</summary>
    public bool RequireHttpsMetadata { get; set; } = true;
}
