using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blueprint.BuildingBlocks.Infrastructure.Messaging.Inbox;

/// <summary>
/// Inbox-Eintrag: merkt sich, welcher Handler welches Integration Event bereits verarbeitet hat (Idempotenz).
/// </summary>
public sealed class InboxMessage
{
    private InboxMessage()
    {
    }

    public Guid EventId { get; private init; }

    public string HandlerName { get; private init; } = string.Empty;

    public DateTimeOffset ProcessedOnUtc { get; private init; }

    public static InboxMessage Create(Guid eventId, string handlerName, DateTimeOffset processedOnUtc) => new()
    {
        EventId = eventId,
        HandlerName = handlerName,
        ProcessedOnUtc = processedOnUtc,
    };
}

internal sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages");
        builder.HasKey(message => new { message.EventId, message.HandlerName });
        builder.Property(message => message.HandlerName).HasMaxLength(300);
    }
}
