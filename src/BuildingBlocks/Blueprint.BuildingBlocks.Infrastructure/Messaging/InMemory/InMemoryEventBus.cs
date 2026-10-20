using System.Threading.Channels;
using Blueprint.BuildingBlocks.Contracts;

namespace Blueprint.BuildingBlocks.Infrastructure.Messaging.InMemory;

/// <summary>
/// In-Memory-Transport auf Basis von <see cref="Channel{T}"/>. Ideal für den Modulith-Start:
/// asynchron und entkoppelt wie ein Broker, aber ohne zusätzliche Infrastruktur.
/// Durch die Outbox geht kein Event beim Speichern verloren; Events, die sich bei einem Absturz noch im Channel befinden,
/// sind allerdings verloren – für Produktionssysteme mit hohen Anforderungen einen Broker verwenden.
/// </summary>
/// <remarks>
/// System.Threading.Channels: https://learn.microsoft.com/dotnet/core/extensions/channels
/// </remarks>
public sealed class InMemoryEventBus : IEventBus
{
    private readonly Channel<IntegrationEvent> _channel = Channel.CreateUnbounded<IntegrationEvent>(
        new UnboundedChannelOptions { SingleReader = true });

    internal ChannelReader<IntegrationEvent> Reader => _channel.Reader;

    public async Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken) =>
        await _channel.Writer.WriteAsync(integrationEvent, cancellationToken);
}
