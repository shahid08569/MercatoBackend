using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Persistence.Configurations;

public class ConversationParticipantConfiguration : IEntityTypeConfiguration<ConversationParticipant>
{
    public void Configure(EntityTypeBuilder<ConversationParticipant> builder)
    {
        builder.HasKey(cp => cp.Id);

        // A user can only be added once as participant in the same conversation
        builder.HasIndex(cp => new { cp.ConversationId, cp.UserId }).IsUnique();

        builder.HasOne(cp => cp.Conversation)
               .WithMany(c => c.Participants)
               .HasForeignKey(cp => cp.ConversationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cp => cp.User)
               .WithMany()
               .HasForeignKey(cp => cp.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}