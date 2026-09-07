using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Persistence.Configurations;

public class CouponUsageConfiguration : IEntityTypeConfiguration<CouponUsage>
{
    public void Configure(EntityTypeBuilder<CouponUsage> builder)
    {
        builder.HasKey(cu => cu.Id);
        // One order can have at most one CouponUsage row (Order supports only 1 coupon)
        builder.HasIndex(cu => cu.OrderId).IsUnique();

        builder.HasOne(cu => cu.Coupon)
               .WithMany(c => c.CouponUsages)
               .HasForeignKey(cu => cu.CouponId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cu => cu.User)
               .WithMany()
               .HasForeignKey(cu => cu.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cu => cu.Order)
               .WithOne(o => o.CouponUsage)
               .HasForeignKey<CouponUsage>(cu => cu.OrderId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}