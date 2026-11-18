using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Persistence.Configurations;

public class AffiliateCommissionRecordConfiguration : IEntityTypeConfiguration<AffiliateCommissionRecord>
{
    public void Configure(EntityTypeBuilder<AffiliateCommissionRecord> builder)
    {
        builder.HasKey(acr => acr.Id);

        // Monetary precision
        builder.Property(acr => acr.SaleAmount).HasColumnType("decimal(18,2)");
        builder.Property(acr => acr.CommissionPercentage).HasColumnType("decimal(5,2)");
        builder.Property(acr => acr.CommissionAmount).HasColumnType("decimal(18,2)");

        builder.Property(acr => acr.AdminOverrideReason).HasMaxLength(500);

        // Relational foreign keys with Restrict delete behavior for financial audit preservation
        builder.HasOne(acr => acr.Order)
               .WithMany()
               .HasForeignKey(acr => acr.OrderId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(acr => acr.Merchant)
               .WithMany()
               .HasForeignKey(acr => acr.MerchantId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(acr => acr.Affiliate)
               .WithMany()
               .HasForeignKey(acr => acr.AffiliateId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}