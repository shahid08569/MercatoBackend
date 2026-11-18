namespace MercatoDomain.Entities;

using MercatoDomain.Enums;

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Associated conversation room
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    // User who sent this message
    public Guid SenderUserId { get; set; }
    public User SenderUser { get; set; } = null!;

    // Text content of the message
    public string Content { get; set; } = string.Empty;

    // Delivery & Read Status (Sent = single tick, Delivered = double tick, Read = blue ticks)
    public MessageDeliveryStatus DeliveryStatus { get; set; } = MessageDeliveryStatus.Sent;

    // Exact Timestamps
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }

    // Edit message tracking
    public bool IsEdited { get; set; } = false;
    public DateTime? EditedAt { get; set; }

    // Delete for everyone
    public bool IsDeletedForEveryone { get; set; } = false;

    // File / Media attachment details
    public string? AttachmentUrl { get; set; }
    public string? AttachmentFileName { get; set; }
    public string? AttachmentType { get; set; } // e.g. "image/jpeg", "application/pdf"
    public long? AttachmentSizeBytes { get; set; }

    // Navigation: Users who deleted this message only for themselves
    public ICollection<ChatMessageUserDeletion> UserDeletions { get; set; } = new List<ChatMessageUserDeletion>();
}