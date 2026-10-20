using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace Blueprint.BuildingBlocks.Infrastructure.Web;

/// <summary>
/// Ein OpenAPI-Dokument pro Modul. Die Dokumente sind die Quelle für die generierten Refit-Clients (Refitter)
/// und bleiben bei einer Auslagerung des Moduls als Microservice unverändert.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Mehrere OpenAPI-Dokumente: https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi#generate-multiple-openapi-documents</item>
/// <item>Build-Time-Generierung: https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi#generate-openapi-documents-at-build-time</item>
/// <item>Transformer (Security Scheme): https://learn.microsoft.com/aspnet/core/fundamentals/openapi/customize-openapi#use-document-transformers</item>
/// </list>
/// </remarks>
public static class ModuleOpenApiExtensions
{
    private const string BearerScheme = "Bearer";

    /// <param name="services">Service Collection.</param>
    /// <param name="moduleName">Modulname = Dokumentname = Gruppenname der Endpunkte (<c>WithGroupName</c>).</param>
    /// <param name="title">Titel des Dokuments.</param>
    public static IServiceCollection AddModuleOpenApi(this IServiceCollection services, string moduleName, string title)
    {
        // Strikte Zahlen im JSON-Vertrag (ASP.NET-Default erlaubt Zahlen als Strings → "number|string" im Schema).
        // Remarks: https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/responses#configure-json-serialization-options
        services.ConfigureHttpJsonOptions(json => json.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);

        return services.AddOpenApi(moduleName, options =>
        {
            // OpenAPI 3.0 statt 3.1: maximale Kompatibilität mit Code-Generatoren (Refitter/NSwag).
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
            options.ShouldInclude = description => string.Equals(description.GroupName, moduleName, StringComparison.OrdinalIgnoreCase);

            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo { Title = title, Version = "v1" };
                document.Servers = [];
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes[BearerScheme] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Access Token von Keycloak (OIDC).",
                };
                return Task.CompletedTask;
            });

            // decimal als "format: decimal" kennzeichnen, damit Generatoren decimal statt double erzeugen.
            options.AddSchemaTransformer((schema, context, _) =>
            {
                if (context.JsonTypeInfo.Type == typeof(decimal) || context.JsonTypeInfo.Type == typeof(decimal?))
                {
                    schema.Format = "decimal";
                }

                return Task.CompletedTask;
            });

            options.AddOperationTransformer((operation, context, _) =>
            {
                var metadata = context.Description.ActionDescriptor.EndpointMetadata;
                var requiresAuthorization = metadata.OfType<IAuthorizeData>().Any() && !metadata.OfType<IAllowAnonymous>().Any();

                if (requiresAuthorization)
                {
                    operation.Security ??= [];
                    operation.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference(BearerScheme, context.Document)] = [],
                    });
                }

                return Task.CompletedTask;
            });
        });
    }
}
