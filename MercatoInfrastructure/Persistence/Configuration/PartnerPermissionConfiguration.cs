using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Persistence.Configurations;

public class PartnerPermissionConfiguration : IEntityTypeConfiguration<PartnerPermission>
{
    public void Configure(EntityTypeBuilder<PartnerPermission> builder)
    {
        builder.HasKey(pp => pp.Id);
        builder.Property(pp => pp.PermissionKey).HasMaxLength(100).IsRequired();

        // Prevent duplicate permission keys for the same partner
        builder.HasIndex(pp => new { pp.PartnerId, pp.PermissionKey }).IsUnique();

        builder.HasOne(pp => pp.Partner)
               .WithMany(p => p.Permissions)
               .HasForeignKey(pp => pp.PartnerId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}