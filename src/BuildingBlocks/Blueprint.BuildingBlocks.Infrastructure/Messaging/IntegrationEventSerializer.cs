using System.Text.Json;
using Blueprint.BuildingBlocks.Contracts;

namespace Blueprint.BuildingBlocks.Infrastructure.Messaging;

/// <summary>Serialisiert Integration Events für Outbox und Transport (System.Text.Json, Web-Defaults).</summary>
/// <remarks>
/// System.Text.Json: https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/overview<br/>
/// Hinweis: Für einen Broker sollte der Typname durch einen stabilen, versionierten Event-Namen ersetzt werden.
/// </remarks>
public static class IntegrationEventSerializer
{
    public static string GetTypeName(Type eventType) =>
        $"{eventType.FullName}, {eventType.Assembly.GetName().Name}";

    public static string Serialize(IntegrationEvent integrationEvent) =>
        JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), JsonSerializerOptions.Web);

    public static IntegrationEvent Deserialize(string typeName, string content)
    {
        var type = Type.GetType(typeName, throwOnError: true)!;
        return (IntegrationEvent)(JsonSerializer.Deserialize(content, type, JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException($"Integration Event '{typeName}' konnte nicht deserialisiert werden."));
    }
}
