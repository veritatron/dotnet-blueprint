namespace Blueprint.BuildingBlocks.Infrastructure.Messaging.Outbox;

/// <summary>Konfiguration des Outbox-Processors (Abschnitt <c>Messaging:Outbox</c>).</summary>
public sealed class OutboxOptions
{
    public const string SectionName = "Messaging:Outbox";

    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(2);

    public int BatchSize { get; set; } = 50;

    public int MaxAttempts { get; set; } = 10;
}
