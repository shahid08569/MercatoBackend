using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Persistence.Configurations;

public class VariantAttributeValueConfiguration : IEntityTypeConfiguration<VariantAttributeValue>
{
    public void Configure(EntityTypeBuilder<VariantAttributeValue> builder)
    {
        builder.HasKey(v => v.Id);
        // Composite UNIQUE: same variant can't have two values for the same attribute
        builder.HasIndex(v => new { v.ProductVariantId, v.VariantAttributeId }).IsUnique();

        builder.HasOne(v => v.ProductVariant)
               .WithMany(pv => pv.VariantAttributeValues)
               .HasForeignKey(v => v.ProductVariantId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.VariantAttribute)
               .WithMany(va => va.VariantAttributeValues)
               .HasForeignKey(v => v.VariantAttributeId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}