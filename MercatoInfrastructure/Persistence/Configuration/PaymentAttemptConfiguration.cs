using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Persistence.Configurations;

public class PaymentAttemptConfiguration : IEntityTypeConfiguration<PaymentAttempt>
{
    public void Configure(EntityTypeBuilder<PaymentAttempt> builder)
    {
        builder.HasKey(pa => pa.Id);
        builder.Property(pa => pa.Amount).HasColumnType("decimal(18,2)");
        builder.Property(pa => pa.Currency).HasMaxLength(3);

        // Filtered UNIQUE: only enforced when ProviderIntentId is NOT NULL
        // (a payment attempt can exist before it has reached the provider)
        builder.HasIndex(pa => pa.ProviderIntentId)
               .IsUnique()
               .HasFilter("[ProviderIntentId] IS NOT NULL");

        builder.HasOne(pa => pa.Order)
               .WithMany(o => o.PaymentAttempts)
               .HasForeignKey(pa => pa.OrderId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}