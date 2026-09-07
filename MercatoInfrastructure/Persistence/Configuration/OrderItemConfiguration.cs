using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(oi => oi.Id);
        builder.Property(oi => oi.UnitPriceAtPurchase).HasColumnType("decimal(18,2)");
        builder.Property(oi => oi.DiscountAppliedAtPurchase).HasColumnType("decimal(18,2)");

        builder.HasOne(oi => oi.Order)
               .WithMany(o => o.OrderItems)
               .HasForeignKey(oi => oi.OrderId)
               .OnDelete(DeleteBehavior.Cascade);

        // Nullable + SetNull: order history survives even if the variant is later removed,
        // because the snapshot fields (ProductNameSnapshot, SkuSnapshot, etc.) hold the truth
        builder.HasOne(oi => oi.ProductVariant)
               .WithMany()
               .HasForeignKey(oi => oi.ProductVariantId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}