using Blueprint.BuildingBlocks.Infrastructure.Persistence;
using Blueprint.Modules.Ordering.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blueprint.Modules.Ordering.Infrastructure.Persistence;

/// <remarks>
/// Owned Entity Types für Teile eines Aggregats: https://learn.microsoft.com/ef/core/modeling/owned-entities<br/>
/// Backing Fields für gekapselte Collections: https://learn.microsoft.com/ef/core/modeling/backing-field
/// </remarks>
internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id).ValueGeneratedNever();
        builder.Property(order => order.CustomerId).HasMaxLength(200).IsRequired();
        builder.HasIndex(order => order.CustomerId);
        builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(order => order.RejectionReason).HasMaxLength(500);
        builder.Property(order => order.Currency).HasMaxLength(3).IsFixedLength();
        builder.Ignore(order => order.Total);
        builder.Property<Guid>(ModuleDbContext.ConcurrencyTokenProperty).IsConcurrencyToken();

        builder.OwnsMany(order => order.Lines, line =>
        {
            line.ToTable("OrderLines");
            line.WithOwner().HasForeignKey("OrderId");
            line.HasKey(orderLine => orderLine.Id);
            line.Property(orderLine => orderLine.Id).ValueGeneratedNever();
            line.Property(orderLine => orderLine.ProductName).HasMaxLength(200).IsRequired();
            line.Property(orderLine => orderLine.UnitPrice).HasPrecision(18, 2);
        });
        builder.Navigation(order => order.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
