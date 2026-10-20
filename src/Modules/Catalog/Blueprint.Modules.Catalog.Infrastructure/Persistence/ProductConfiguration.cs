using Blueprint.BuildingBlocks.Infrastructure.Persistence;
using Blueprint.Modules.Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blueprint.Modules.Catalog.Infrastructure.Persistence;

/// <summary>Mapping des Product-Aggregats (Fluent API statt Attribute → Domain bleibt persistenz-ignorant).</summary>
/// <remarks>
/// https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-implementation-entity-framework-core#infrastructure-in-entity-framework-core-from-a-ddd-perspective
/// </remarks>
internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Id).ValueGeneratedNever();
        builder.Property(product => product.Sku).HasMaxLength(32).IsRequired();
        builder.HasIndex(product => product.Sku).IsUnique();
        builder.Property(product => product.Name).HasMaxLength(200).IsRequired();
        builder.Property(product => product.Description).HasMaxLength(2000);

        builder.ComplexProperty(product => product.Price, price =>
        {
            price.Property(money => money.Amount).HasColumnName("Price").HasPrecision(18, 2);
            price.Property(money => money.Currency).HasColumnName("Currency").HasMaxLength(3).IsFixedLength();
        });

        // Optimistische Nebenläufigkeit (Bestand!) über eine app-verwaltete Versionsspalte (siehe ModuleDbContext).
        builder.Property<Guid>(ModuleDbContext.ConcurrencyTokenProperty).IsConcurrencyToken();
    }
}
