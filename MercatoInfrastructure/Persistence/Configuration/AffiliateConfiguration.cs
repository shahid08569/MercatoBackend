using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Persistence.Configurations;

public class AffiliateConfiguration : IEntityTypeConfiguration<Affiliate>
{
    public void Configure(EntityTypeBuilder<Affiliate> builder)
    {
        builder.HasKey(a => a.Id);

        // Unique referral code for tracking links (e.g., ?ref=AFF-7860)
        builder.Property(a => a.AffiliateCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(a => a.AffiliateCode).IsUnique();

        builder.Property(a => a.PayoutMethod).HasMaxLength(50);
        builder.Property(a => a.PayoutAccountNumber).HasMaxLength(100);

        builder.HasOne(a => a.User)
               .WithMany()
               .HasForeignKey(a => a.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}