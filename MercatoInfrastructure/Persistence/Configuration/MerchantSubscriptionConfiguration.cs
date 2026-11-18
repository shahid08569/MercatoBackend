using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Persistence.Configurations;

public class MerchantSubscriptionConfiguration : IEntityTypeConfiguration<MerchantSubscription>
{
    public void Configure(EntityTypeBuilder<MerchantSubscription> builder)
    {
        builder.HasKey(ms => ms.Id);

        builder.HasOne(ms => ms.Merchant)
               .WithMany()
               .HasForeignKey(ms => ms.MerchantId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ms => ms.SubscriptionPlan)
               .WithMany()
               .HasForeignKey(ms => ms.SubscriptionPlanId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}