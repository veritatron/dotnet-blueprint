using Blueprint.BuildingBlocks.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blueprint.BuildingBlocks.Infrastructure.Messaging.Outbox;

/// <summary>Ein noch zu veröffentlichendes Integration Event (Transactional Outbox).</summary>
public sealed class OutboxMessage
{
    private OutboxMessage()
    {
    }

    public Guid Id { get; private init; }

    public string Type { get; private init; } = string.Empty;

    public string Content { get; private init; } = string.Empty;

    public DateTimeOffset OccurredOnUtc { get; private init; }

    public DateTimeOffset? ProcessedOnUtc { get; private set; }

    public int Attempts { get; private set; }

    public string? Error { get; private set; }

    public static OutboxMessage Create(IntegrationEvent integrationEvent) => new()
    {
        Id = integrationEvent.Id,
        Type = IntegrationEventSerializer.GetTypeName(integrationEvent.GetType()),
        Content = IntegrationEventSerializer.Serialize(integrationEvent),
        OccurredOnUtc = integrationEvent.OccurredOnUtc,
    };

    public void MarkProcessed(DateTimeOffset processedOnUtc)
    {
        ProcessedOnUtc = processedOnUtc;
        Error = null;
    }

    public void MarkFailed(string error)
    {
        Attempts++;
        Error = error.Length > 2000 ? error[..2000] : error;
    }
}

/// <param name="isOracle">
/// Oracle bildet unbegrenzte Strings auf NVARCHAR2(2000) ab – für Event-Payloads wird daher explizit NCLOB verwendet.
/// Remarks: https://docs.oracle.com/en/database/oracle/oracle-database/23/odpnt/EFCoreDataTypeMapping.html
/// </param>
internal sealed class OutboxMessageConfiguration(bool isOracle) : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Type).HasMaxLength(500).IsRequired();
        var content = builder.Property(message => message.Content).IsRequired();
        if (isOracle)
        {
            content.HasColumnType("NCLOB");
        }

        builder.Property(message => message.Error).HasMaxLength(2000);
        builder.HasIndex(message => new { message.ProcessedOnUtc, message.OccurredOnUtc });
    }
}
