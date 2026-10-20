using Blueprint.BuildingBlocks.Application.Messaging;
using Blueprint.BuildingBlocks.Contracts;
using Blueprint.BuildingBlocks.Infrastructure.DomainEvents;
using Blueprint.BuildingBlocks.Infrastructure.Messaging.Inbox;
using Blueprint.BuildingBlocks.Infrastructure.Messaging.InMemory;
using Blueprint.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Retry;

namespace Blueprint.BuildingBlocks.Infrastructure.Messaging;

/// <summary>Konfiguration des Event Bus (Abschnitt <c>Messaging</c>).</summary>
public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";

    /// <summary>Transport: <c>InMemory</c>. Erweiterungspunkte: <c>RabbitMQ</c>, <c>Kafka</c>, <c>AzureServiceBus</c>.</summary>
    public string Transport { get; set; } = "InMemory";
}

public static class MessagingExtensions
{
    public const string IntegrationEventResiliencePipeline = "integration-events";

    /// <summary>
    /// Registriert Domain-Event-Dispatching, den Event Bus (Transport per Konfiguration) und die Resilience-Pipeline
    /// für Integration-Event-Handler.
    /// </summary>
    /// <remarks>
    /// Resilience mit Microsoft.Extensions.Resilience: https://learn.microsoft.com/dotnet/core/resilience/
    /// </remarks>
    public static IHostApplicationBuilder AddEventBus(this IHostApplicationBuilder builder)
    {
        var options = builder.Configuration.GetSection(MessagingOptions.SectionName).Get<MessagingOptions>() ?? new MessagingOptions();

        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        builder.Services.AddResiliencePipeline(IntegrationEventResiliencePipeline, pipeline => pipeline
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromMilliseconds(200),
                ShouldHandle = new PredicateBuilder().Handle<Exception>(exception => exception is not OperationCanceledException),
            }));

        switch (options.Transport)
        {
            case "InMemory":
                builder.Services.AddSingleton<InMemoryEventBus>();
                builder.Services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<InMemoryEventBus>());
                builder.Services.AddHostedService<InMemoryEventBusConsumer>();
                break;

            // Erweiterungspunkt: z. B. "RabbitMQ" → IEventBus-Implementierung + Consumer-BackgroundService,
            // der pro IntegrationEventSubscription eine Queue bindet. Siehe docs/messaging.md.
            default:
                throw new InvalidOperationException($"Event-Bus-Transport '{options.Transport}' ist nicht registriert.");
        }

        return builder;
    }

    /// <summary>
    /// Abonniert ein Integration Event für ein Modul. Der Handler wird mit dem Inbox-Decorator umhüllt.
    /// </summary>
    public static IServiceCollection AddIntegrationEventHandler<TEvent, THandler, TContext>(
        this IServiceCollection services,
        string moduleName)
        where TEvent : IntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
        where TContext : ModuleDbContext
    {
        services.AddScoped<THandler>();
        services.AddScoped<InboxIntegrationEventHandler<TEvent, THandler, TContext>>();
        services.AddSingleton(new IntegrationEventSubscription(
            typeof(TEvent),
            typeof(InboxIntegrationEventHandler<TEvent, THandler, TContext>),
            moduleName));
        return services;
    }
}
