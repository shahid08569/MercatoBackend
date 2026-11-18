using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(p => p.Name); // Speeds up search

        // Decimal precision for financial fields
        builder.Property(p => p.Price).HasColumnType("decimal(18,2)");
        builder.Property(p => p.AvgRating).HasColumnType("decimal(3,2)");
        builder.Property(p => p.CustomAffiliateCommissionRate).HasColumnType("decimal(5,2)");

        // Category & Brand relationships
        builder.HasOne(p => p.Category)
               .WithMany(c => c.Products)
               .HasForeignKey(p => p.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Brand)
               .WithMany(b => b.Products)
               .HasForeignKey(p => p.BrandId)
               .OnDelete(DeleteBehavior.Restrict);

        // Multi-Vendor Merchant ownership (B2C & B2B)
        builder.HasOne(p => p.Merchant)
               .WithMany(m => m.Products)
               .HasForeignKey(p => p.MerchantId)
               .OnDelete(DeleteBehavior.Restrict);

        // C2C individual seller ownership (Customer pre-owned item)
        builder.HasOne(p => p.C2CSellerUser)
               .WithMany()
               .HasForeignKey(p => p.C2CSellerUserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}