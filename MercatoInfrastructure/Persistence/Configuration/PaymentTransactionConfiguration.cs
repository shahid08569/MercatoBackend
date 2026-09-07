using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Persistence.Configurations;

public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.HasKey(pt => pt.Id);
        builder.Property(pt => pt.ProviderReferenceId).HasMaxLength(150).IsRequired();
        builder.HasIndex(pt => pt.ProviderReferenceId).IsUnique();
        builder.Property(pt => pt.Amount).HasColumnType("decimal(18,2)");
        builder.Property(pt => pt.Currency).HasMaxLength(3);

        builder.HasOne(pt => pt.PaymentAttempt)
               .WithMany(pa => pa.PaymentTransactions)
               .HasForeignKey(pt => pt.PaymentAttemptId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}