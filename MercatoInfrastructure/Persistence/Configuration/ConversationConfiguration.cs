using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Persistence.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Title).HasMaxLength(200);

        // Indexes for fast querying of rooms
        builder.HasIndex(c => c.Type);
        builder.HasIndex(c => c.LastMessageAt);

        // Context boundary foreign keys
        builder.HasOne(c => c.Order)
               .WithMany()
               .HasForeignKey(c => c.OrderId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Merchant)
               .WithMany()
               .HasForeignKey(c => c.MerchantId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Partner)
               .WithMany()
               .HasForeignKey(c => c.PartnerId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}