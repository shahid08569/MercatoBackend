using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Persistence.Configurations;

public class B2BTieredPriceConfiguration : IEntityTypeConfiguration<B2BTieredPrice>
{
    public void Configure(EntityTypeBuilder<B2BTieredPrice> builder)
    {
        builder.HasKey(tp => tp.Id);

        // Discounted wholesale unit price
        builder.Property(tp => tp.UnitPrice).HasColumnType("decimal(18,2)");

        // Deleting a product removes its wholesale volume tiers
        builder.HasOne(tp => tp.Product)
               .WithMany(p => p.B2BTieredPrices)
               .HasForeignKey(tp => tp.ProductId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}