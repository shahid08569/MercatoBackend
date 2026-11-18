using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Persistence.Configurations;

public class MerchantConfiguration : IEntityTypeConfiguration<Merchant>
{
    public void Configure(EntityTypeBuilder<Merchant> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.StoreName).HasMaxLength(150).IsRequired();

        // Unique store slug for URL routing (e.g. mercato.com/store/tech-zone)
        builder.Property(m => m.StoreSlug).HasMaxLength(150).IsRequired();
        builder.HasIndex(m => m.StoreSlug).IsUnique();

        builder.Property(m => m.BusinessRegNumber).HasMaxLength(50);

        // Rates precision
        builder.Property(m => m.DefaultAffiliateCommissionRate).HasColumnType("decimal(5,2)");
        builder.Property(m => m.CustomPlatformCommissionRate).HasColumnType("decimal(5,2)");

        // Owner User link
        builder.HasOne(m => m.OwnerUser)
               .WithMany()
               .HasForeignKey(m => m.OwnerUserId)
               .OnDelete(DeleteBehavior.Restrict);

        // Optional Partner link (if onboarded by a partner)
        builder.HasOne(m => m.Partner)
               .WithMany()
               .HasForeignKey(m => m.PartnerId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}