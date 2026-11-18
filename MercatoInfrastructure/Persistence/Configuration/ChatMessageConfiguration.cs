using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Persistence.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Content).HasMaxLength(2000).IsRequired();
        builder.Property(m => m.AttachmentUrl).HasMaxLength(500);

        // High-performance index for loading conversation thread chronologically
        builder.HasIndex(m => m.ConversationId);
        builder.HasIndex(m => m.SentAt);

        builder.HasOne(m => m.Conversation)
               .WithMany(c => c.Messages)
               .HasForeignKey(m => m.ConversationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.SenderUser)
               .WithMany()
               .HasForeignKey(m => m.SenderUserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}