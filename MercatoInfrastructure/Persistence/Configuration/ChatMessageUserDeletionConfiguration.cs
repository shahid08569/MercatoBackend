using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Persistence.Configurations;

public class ChatMessageUserDeletionConfiguration : IEntityTypeConfiguration<ChatMessageUserDeletion>
{
    public void Configure(EntityTypeBuilder<ChatMessageUserDeletion> builder)
    {
        builder.HasKey(d => d.Id);

        // A user only records deletion once per message
        builder.HasIndex(d => new { d.ChatMessageId, d.UserId }).IsUnique();

        builder.HasOne(d => d.ChatMessage)
               .WithMany(m => m.UserDeletions)
               .HasForeignKey(d => d.ChatMessageId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.User)
               .WithMany()
               .HasForeignKey(d => d.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}