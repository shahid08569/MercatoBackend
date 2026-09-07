using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MercatoInfrastructure.Persistence.Configurations;

public class VariantAttributeConfiguration : IEntityTypeConfiguration<MercatoDomain.Entities.VariantAttribute>
{
    public void Configure(EntityTypeBuilder<MercatoDomain.Entities.VariantAttribute> builder)
    {
        builder.HasKey(va => va.Id);
        builder.Property(va => va.Name).HasMaxLength(50).IsRequired();
        builder.HasIndex(va => va.Name).IsUnique(); // VariantAttribute.Name UNIQUE
    }
}