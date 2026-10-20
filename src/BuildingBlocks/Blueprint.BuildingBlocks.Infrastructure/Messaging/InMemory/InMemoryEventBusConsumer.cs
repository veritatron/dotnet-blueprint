using Blueprint.BuildingBlocks.Contracts;
using Blueprint.BuildingBlocks.Infrastructure.Messaging.Inbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly.Registry;

namespace Blueprint.BuildingBlocks.Infrastructure.Messaging.InMemory;

/// <summary>
/// Liest Events aus dem <see cref="InMemoryEventBus"/> und stellt sie allen Abonnenten zu –
/// jeweils in einem eigenen DI-Scope (eigene Unit of Work), analog zu einem Broker-Consumer.
/// </summary>
/// <remarks>
/// https://learn.microsoft.com/dotnet/core/extensions/queue-service
/// </remarks>
internal sealed partial class InMemoryEventBusConsumer(
    InMemoryEventBus eventBus,
    IEnumerable<IntegrationEventSubscription> subscriptions,
    IServiceScopeFactory scopeFactory,
    ResiliencePipelineProvider<string> pipelineProvider,
    ILogger<InMemoryEventBusConsumer> logger) : BackgroundService
{
    private readonly ILookup<Type, IntegrationEventSubscription> _subscriptions = subscriptions.ToLookup(s => s.EventType);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pipeline = pipelineProvider.GetPipeline(MessagingExtensions.IntegrationEventResiliencePipeline);

        await foreach (var integrationEvent in eventBus.Reader.ReadAllAsync(stoppingToken))
        {
            foreach (var subscription in _subscriptions[integrationEvent.GetType()])
            {
                try
                {
                    await pipeline.ExecuteAsync(
                        async token => await DispatchAsync(subscription, integrationEvent, token),
                        stoppingToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    // Bei einem Broker würde die Nachricht hier in eine Dead-Letter-Queue wandern.
                    LogHandlingFailed(exception, integrationEvent.GetType().Name, integrationEvent.Id, subscription.ModuleName);
                }
            }
        }
    }

    private async ValueTask DispatchAsync(IntegrationEventSubscription subscription, IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var handler = (IIntegrationEventDispatchTarget)scope.ServiceProvider.GetRequiredService(subscription.DispatchTargetType);
        await handler.HandleAsync(integrationEvent, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Integration Event {EventType} ({EventId}) konnte von Modul {Module} nicht verarbeitet werden")]
    private partial void LogHandlingFailed(Exception exception, string eventType, Guid eventId, string module);
}
