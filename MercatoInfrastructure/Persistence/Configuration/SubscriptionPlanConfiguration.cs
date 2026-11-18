using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Persistence.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.HasKey(sp => sp.Id);
        builder.Property(sp => sp.Name).HasMaxLength(100).IsRequired();

        // Monthly fee and discount percentage
        builder.Property(sp => sp.PriceMonthly).HasColumnType("decimal(18,2)");
        builder.Property(sp => sp.PlatformCommissionDiscountRate).HasColumnType("decimal(5,2)");
    }
}