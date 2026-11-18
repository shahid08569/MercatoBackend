using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Persistence.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.HasKey(w => w.Id);

        // Monetary precision for balances
        builder.Property(w => w.AvailableBalance).HasColumnType("decimal(18,2)");
        builder.Property(w => w.PendingEscrowBalance).HasColumnType("decimal(18,2)");

        builder.Property(w => w.Currency).HasMaxLength(10).IsRequired();

        // Enforce unique wallet per user & owner-type combination
        builder.HasIndex(w => new { w.UserId, w.OwnerType }).IsUnique();

        builder.HasOne(w => w.User)
               .WithMany()
               .HasForeignKey(w => w.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}