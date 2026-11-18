using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Persistence.Configurations;

public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.HasKey(wt => wt.Id);

        // Immutable financial values
        builder.Property(wt => wt.Amount).HasColumnType("decimal(18,2)");
        builder.Property(wt => wt.BalanceAfter).HasColumnType("decimal(18,2)");

        builder.Property(wt => wt.Description).HasMaxLength(300).IsRequired();

        // Indexes for high-performance lookup of ledger history
        builder.HasIndex(wt => wt.WalletId);
        builder.HasIndex(wt => wt.ReferenceId);

        builder.HasOne(wt => wt.Wallet)
               .WithMany(w => w.Transactions)
               .HasForeignKey(wt => wt.WalletId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}