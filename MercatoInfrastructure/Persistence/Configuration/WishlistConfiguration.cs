using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Persistence.Configurations;

public class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
{
    public void Configure(EntityTypeBuilder<Wishlist> builder)
    {
        builder.HasKey(w => w.Id);

        builder.HasOne(w => w.User)
               .WithOne()
               .HasForeignKey<Wishlist>(w => w.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(w => w.UserId).IsUnique(); // Wishlist.UserId UNIQUE
    }
}