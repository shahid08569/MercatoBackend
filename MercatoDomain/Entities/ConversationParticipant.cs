namespace MercatoDomain.Entities;

public class ConversationParticipant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Associated conversation room
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    // User participating in this conversation
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Timestamp when this user last read messages in this room (used for unread counters)
    public DateTime? LastReadAt { get; set; }

    // User preference to mute notifications for this room
    public bool IsMuted { get; set; } = false;
}