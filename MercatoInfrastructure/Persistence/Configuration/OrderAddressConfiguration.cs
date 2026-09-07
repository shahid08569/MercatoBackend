using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Persistence.Configurations;

public class OrderAddressConfiguration : IEntityTypeConfiguration<OrderAddress>
{
    public void Configure(EntityTypeBuilder<OrderAddress> builder)
    {
        builder.HasKey(oa => oa.Id);
        // Composite UNIQUE: one order can't have two "Shipping" rows
        builder.HasIndex(oa => new { oa.OrderId, oa.Type }).IsUnique();

        builder.HasOne(oa => oa.Order)
               .WithMany(o => o.OrderAddresses)
               .HasForeignKey(oa => oa.OrderId)
               .OnDelete(DeleteBehavior.Cascade);

        // No FK to Address on purpose - this is a fully self-contained snapshot
    }
}