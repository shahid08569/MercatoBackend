using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;
namespace MercatoInfrastructure.Persistence.Configurations;
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken> 
{ 
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.TokenHash).HasMaxLength(256).IsRequired();
        builder.HasIndex(rt => rt.TokenHash).IsUnique();

        builder.HasOne(rt => rt.User)
               .WithMany(u => u.RefreshTokens)
               .HasForeignKey(rt => rt.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        // Self-relationship: ek RefreshToken doosre RefreshToken ko "replace" karta hai
        builder.HasOne(rt => rt.ReplacedByToken)
               .WithMany()
               .HasForeignKey(rt => rt.ReplacedByTokenId)
               .OnDelete(DeleteBehavior.Restrict);
    }
} 