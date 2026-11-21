using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.OrderNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(o => o.OrderNumber).IsUnique(); // Order.OrderNumber UNIQUE

        // Composite UNIQUE: idempotency is scoped per user, not global
        builder.HasIndex(o => new { o.UserId, o.IdempotencyKey }).IsUnique();

        builder.Property(o => o.Subtotal).HasColumnType("decimal(18,2)");
        builder.Property(o => o.Discount).HasColumnType("decimal(18,2)");
        builder.Property(o => o.Tax).HasColumnType("decimal(18,2)");
        builder.Property(o => o.ShippingCost).HasColumnType("decimal(18,2)");
        builder.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");

        // Tracking & Courier details
        builder.Property(o => o.TrackingNumber).HasMaxLength(100);
        builder.Property(o => o.ShippingCourier).HasMaxLength(100);

        builder.HasOne(o => o.User)
               .WithMany()
               .HasForeignKey(o => o.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        // Multi-Vendor Hierarchy: Parent-to-SubOrders self-referencing relationship
        builder.HasOne(o => o.ParentOrder)
               .WithMany(o => o.SubOrders)
               .HasForeignKey(o => o.ParentOrderId)
               .OnDelete(DeleteBehavior.Restrict);

        // Merchant ownership for Sub-Orders
        builder.HasOne(o => o.Merchant)
               .WithMany()
               .HasForeignKey(o => o.MerchantId)
               .OnDelete(DeleteBehavior.Restrict);

        // Nullable FKs use SetNull - historical order data must survive
        // even if the referenced CheckoutSession/Coupon is later removed
        builder.HasOne(o => o.CheckoutSession)
               .WithOne(cs => cs.Order)
               .HasForeignKey<Order>(o => o.CheckoutSessionId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(o => o.Coupon)
               .WithMany()
               .HasForeignKey(o => o.CouponId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}