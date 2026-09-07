using MercatoDomain.Entities;
using MercatoDomain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MercatoInfrastructure.Persistence.Configurations;

public class CheckoutSessionConfiguration : IEntityTypeConfiguration<CheckoutSession>
{
    public void Configure(EntityTypeBuilder<CheckoutSession> builder)
    {
        builder.HasKey(cs => cs.Id);
        builder.Property(cs => cs.ComputedTotal).HasColumnType("decimal(18,2)");

        builder.HasOne(cs => cs.User)
               .WithMany()
               .HasForeignKey(cs => cs.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cs => cs.Address)
               .WithMany()
               .HasForeignKey(cs => cs.AddressId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}