using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Persistence.Configurations;

public class PartnerAffiliateConfiguration : IEntityTypeConfiguration<PartnerAffiliate>
{
    public void Configure(EntityTypeBuilder<PartnerAffiliate> builder)
    {
        builder.HasKey(pa => pa.Id);

        // Prevent linking the same affiliate to the same partner multiple times
        builder.HasIndex(pa => new { pa.PartnerId, pa.AffiliateId }).IsUnique();

        // Commission rate precision (e.g. 5.50%)
        builder.Property(pa => pa.CommissionRate).HasColumnType("decimal(5,2)");

        // Foreign keys with Restrict delete behavior
        builder.HasOne(pa => pa.Partner)
               .WithMany()
               .HasForeignKey(pa => pa.PartnerId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pa => pa.Affiliate)
               .WithMany(a => a.PartnerLinks)
               .HasForeignKey(pa => pa.AffiliateId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}