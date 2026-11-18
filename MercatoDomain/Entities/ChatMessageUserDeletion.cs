namespace MercatoDomain.Entities;

public class ChatMessageUserDeletion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // The message being hidden
    public Guid ChatMessageId { get; set; }
    public ChatMessage ChatMessage { get; set; } = null!;

    // The specific user who deleted it from their view
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
}