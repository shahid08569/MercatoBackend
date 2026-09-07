using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(al => al.Id);

        // Nullable + SetNull: audit history survives even if the actor's account is later removed
        builder.HasOne(al => al.User)
               .WithMany()
               .HasForeignKey(al => al.UserId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}