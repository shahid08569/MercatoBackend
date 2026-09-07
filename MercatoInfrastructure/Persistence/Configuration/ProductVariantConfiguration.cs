using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Persistence.Configurations;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.HasKey(pv => pv.Id);
        builder.Property(pv => pv.SKU).HasMaxLength(50).IsRequired();
        builder.HasIndex(pv => pv.SKU).IsUnique(); // ProductVariant.SKU UNIQUE
        builder.Property(pv => pv.PriceModifier).HasColumnType("decimal(18,2)");

        builder.HasOne(pv => pv.Product)
               .WithMany(p => p.ProductVariants)
               .HasForeignKey(pv => pv.ProductId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}