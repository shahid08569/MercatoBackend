using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;
namespace MercatoInfrastructure.Persistence.Configurations;
public class AddressConfiguration : IEntityTypeConfiguration<Address> 
{
    public void Configure(EntityTypeBuilder<Address> builder) 
    {
        builder.HasKey(a => a.Id);

        builder.HasOne(a => a.User)
               .WithMany(u => u.Addresses)
               .HasForeignKey(a => a.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
