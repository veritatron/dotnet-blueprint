namespace Blueprint.BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// Abonnement: welches Integration Event wird von welchem (Inbox-dekorierten) Handler verarbeitet.
/// Entspricht bei einem Broker einer Queue/Consumer-Group pro Modul.
/// </summary>
public sealed record IntegrationEventSubscription(Type EventType, Type DispatchTargetType, string ModuleName);
