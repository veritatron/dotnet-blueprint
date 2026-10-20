using Blueprint.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Blueprint.BuildingBlocks.Infrastructure.Messaging.Outbox;

/// <summary>
/// Hintergrunddienst, der ungesendete Outbox-Nachrichten eines Moduls an den Event Bus übergibt (at-least-once).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Background tasks mit Hosted Services: https://learn.microsoft.com/aspnet/core/fundamentals/host/hosted-services</item>
/// <item>Scoped Services in BackgroundService: https://learn.microsoft.com/dotnet/core/extensions/scoped-service</item>
/// <item>Outbox-Muster in eShop: https://learn.microsoft.com/dotnet/architecture/microservices/multi-container-microservice-net-applications/subscribe-events#implementing-atomicity-when-publishing-integration-events-through-the-event-bus</item>
/// </list>
/// Hinweis für mehrere Instanzen: Nachrichten per <c>SELECT ... FOR UPDATE SKIP LOCKED</c> sperren (Oracle und PostgreSQL unterstützen das).
/// </remarks>
internal sealed partial class OutboxProcessor<TContext>(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    TimeProvider timeProvider,
    ILogger<OutboxProcessor<TContext>> logger) : BackgroundService
    where TContext : ModuleDbContext
{
    private readonly OutboxOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.PollingInterval, timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogBatchFailed(exception, typeof(TContext).Name);
            }
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        var messages = await dbContext.OutboxMessages
            .Where(message => message.ProcessedOnUtc == null && message.Attempts < _options.MaxAttempts)
            .OrderBy(message => message.OccurredOnUtc)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
        {
            return;
        }

        foreach (var message in messages)
        {
            try
            {
                var integrationEvent = IntegrationEventSerializer.Deserialize(message.Type, message.Content);
                await eventBus.PublishAsync(integrationEvent, cancellationToken);
                message.MarkProcessed(timeProvider.GetUtcNow());
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogPublishFailed(exception, message.Id, message.Type);
                message.MarkFailed(exception.ToString());
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Outbox-Verarbeitung für {Context} fehlgeschlagen")]
    private partial void LogBatchFailed(Exception exception, string context);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Outbox-Nachricht {MessageId} ({Type}) konnte nicht veröffentlicht werden")]
    private partial void LogPublishFailed(Exception exception, Guid messageId, string type);
}

internal static class OutboxServiceCollectionExtensions
{
    public static IServiceCollection AddOutboxProcessor<TContext>(this IServiceCollection services, IConfiguration configuration)
        where TContext : ModuleDbContext
    {
        services.AddOptions<OutboxOptions>().Bind(configuration.GetSection(OutboxOptions.SectionName));
        services.AddHostedService<OutboxProcessor<TContext>>();
        return services;
    }
}
