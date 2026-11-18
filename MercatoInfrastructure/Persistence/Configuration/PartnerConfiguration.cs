using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Persistence.Configurations;

public class PartnerConfiguration : IEntityTypeConfiguration<Partner>
{
    public void Configure(EntityTypeBuilder<Partner> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.CompanyName).HasMaxLength(150).IsRequired();
        builder.Property(p => p.AssignedTerritory).HasMaxLength(100);

        // Revenue share percentage (e.g. 5.50%)
        builder.Property(p => p.RevenueSharePercentage).HasColumnType("decimal(5,2)");

        // One User can be linked as Partner
        builder.HasOne(p => p.User)
               .WithMany()
               .HasForeignKey(p => p.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}