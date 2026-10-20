# Messaging: Domain Events, Integration Events, Outbox & Inbox

## Begriffe

| | Domain Event | Integration Event |
|---|---|---|
| Sichtbarkeit | modul-intern (`*.Domain`) | öffentlich (`*.Contracts`) |
| Zustellung | synchron, in-process, **vor** dem Commit | asynchron über `IEventBus`, **nach** dem Commit |
| Transaktion | dieselbe Unit of Work | eigene Unit of Work pro Empfänger |
| Beispiel | `OrderPlacedDomainEvent` | `OrderPlacedIntegrationEvent` |

Remarks: [Domain events: design and implementation](https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation),
[Integration event-based communication](https://learn.microsoft.com/dotnet/architecture/microservices/multi-container-microservice-net-applications/integration-event-based-microservice-communications)

## Ablauf

```
Command-Handler
  └─ Aggregate.Raise(DomainEvent)
       └─ ModuleDbContext.SaveChangesAsync()
            ├─ IDomainEventHandler<T>  →  unitOfWork.Enqueue(IntegrationEvent)   ← schreibt in OutboxMessages
            └─ COMMIT (fachliche Daten + Outbox atomar)

OutboxProcessor<TContext> (BackgroundService, je Modul)
  └─ liest unverarbeitete OutboxMessages → IEventBus.PublishAsync → markiert verarbeitet

Event-Bus-Consumer
  └─ pro IntegrationEventSubscription: neuer DI-Scope + Resilience-Retry
       └─ InboxIntegrationEventHandler (Decorator)
            ├─ bereits in InboxMessages? → überspringen (Idempotenz)
            ├─ IIntegrationEventHandler<T>.HandleAsync
            └─ InboxMessage + Änderungen des Handlers in EINER Transaktion speichern
```

Garantien: **at-least-once** Zustellung, **idempotente** Verarbeitung, kein „Dual Write“-Problem.

## Regeln für Handler

- Integration-Event-Handler rufen **nicht** `SaveChangesAsync` auf – das übernimmt der Inbox-Decorator.
- Integration Events sind öffentliche Verträge: nur **abwärtskompatible** Änderungen (Felder hinzufügen, nie umbenennen/entfernen).
  Für Breaking Changes ein neues Event (z. B. `…V2`) einführen.
- Registrierung im Modul: `services.AddIntegrationEventHandler<TEvent, THandler, TModuleDbContext>(moduleName)`.

## In-Memory-Transport (Start)

`InMemoryEventBus` basiert auf `System.Threading.Channels` ([Doku](https://learn.microsoft.com/dotnet/core/extensions/channels)).
Er verhält sich wie ein Broker (asynchron, Consumer pro Modul), benötigt aber keine Infrastruktur.
Einschränkung: Events, die bereits aus der Outbox gelesen, aber noch nicht verarbeitet wurden, gehen bei einem Prozessabsturz verloren.
Für hohe Anforderungen einen Broker verwenden.

## Broker anbinden (RabbitMQ, Kafka, Azure Service Bus …)

1. Neues Projekt, z. B. `Blueprint.BuildingBlocks.Messaging.RabbitMQ`, mit
   - `RabbitMqEventBus : IEventBus` (serialisiert per `IntegrationEventSerializer`, Routing-Key = Event-Typ),
   - `RabbitMqConsumer : BackgroundService`, der für jede `IntegrationEventSubscription` eine Queue `<modul>.<event>` bindet
     und Nachrichten an den `IIntegrationEventDispatchTarget`-Typ der Subscription übergibt (neuer Scope pro Nachricht).
2. In `MessagingExtensions.AddEventBus` einen `case "RabbitMQ":` ergänzen.
3. Im Aspire-AppHost den Broker hinzufügen, z. B. `builder.AddRabbitMQ("messaging")` bzw. `builder.AddKafka("messaging")`, und per `WithReference` an die Services geben.
4. Konfiguration: `Messaging:Transport=RabbitMQ`.

Module, Handler, Outbox und Inbox bleiben unverändert.

Remarks:
- [Aspire RabbitMQ integration](https://learn.microsoft.com/dotnet/aspire/messaging/rabbitmq-integration)
- [Aspire Apache Kafka integration](https://learn.microsoft.com/dotnet/aspire/messaging/kafka-integration)
- [Aspire Azure Service Bus integration](https://learn.microsoft.com/dotnet/aspire/messaging/azure-service-bus-integration)
- [eShop – EventBusRabbitMQ (Referenzimplementierung)](https://github.com/dotnet/eShop/tree/main/src/EventBusRabbitMQ)

## Produktionshinweise

- **Mehrere Instanzen:** Outbox-Nachrichten beim Lesen sperren (`SELECT … FOR UPDATE SKIP LOCKED`, von Oracle und PostgreSQL unterstützt),
  damit nicht mehrere Instanzen dieselbe Nachricht veröffentlichen.
- **Stabile Event-Namen:** Der Outbox-Typname ist aktuell `Namespace.Typ, Assembly`. Für Broker zwischen unabhängig deployten Services
  einen stabilen, versionierten Namen verwenden (z. B. Attribut `[IntegrationEventName("ordering.order-placed.v1")]`).
- **Aufräumen:** verarbeitete Outbox-/Inbox-Einträge periodisch löschen.
- **Dead Letters:** Nach `Messaging:Outbox:MaxAttempts` bleibt eine Nachricht mit Fehlertext in der Outbox stehen → Monitoring/Alerting.
