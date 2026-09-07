using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Persistence.Configurations;

public class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.HasKey(sr => sr.Id);

        builder.HasOne(sr => sr.Order)
               .WithMany(o => o.StockReservations)
               .HasForeignKey(sr => sr.OrderId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sr => sr.ProductVariant)
               .WithMany()
               .HasForeignKey(sr => sr.ProductVariantId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}