using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Persistence.Configurations;

public class MerchantAffiliateConfiguration : IEntityTypeConfiguration<MerchantAffiliate>
{
    public void Configure(EntityTypeBuilder<MerchantAffiliate> builder)
    {
        builder.HasKey(ma => ma.Id);

        // An affiliate can only be linked to a specific merchant once
        builder.HasIndex(ma => new { ma.MerchantId, ma.AffiliateId }).IsUnique();

        builder.Property(ma => ma.CustomCommissionRate).HasColumnType("decimal(5,2)");

        builder.HasOne(ma => ma.Merchant)
               .WithMany()
               .HasForeignKey(ma => ma.MerchantId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ma => ma.Affiliate)
               .WithMany(a => a.MerchantLinks)
               .HasForeignKey(ma => ma.AffiliateId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}