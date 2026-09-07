using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Persistence.Configurations;

public class InventoryAdjustmentConfiguration : IEntityTypeConfiguration<InventoryAdjustment>
{
    public void Configure(EntityTypeBuilder<InventoryAdjustment> builder)
    {
        builder.HasKey(ia => ia.Id);

        builder.HasOne(ia => ia.ProductVariant)
               .WithMany()
               .HasForeignKey(ia => ia.ProductVariantId)
               .OnDelete(DeleteBehavior.Restrict);

        // CreatedByUserId is nullable - null means "system", not a specific admin
        builder.HasOne(ia => ia.CreatedByUser)
               .WithMany()
               .HasForeignKey(ia => ia.CreatedByUserId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}