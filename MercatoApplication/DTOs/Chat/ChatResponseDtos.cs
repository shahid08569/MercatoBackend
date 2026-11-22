namespace MercatoApplication.DTOs.Chat;

// Conversation room card (Inbox view)
public class ConversationResponseDto
{
    public Guid ConversationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ConversationType { get; set; } = string.Empty;

    // Associated context
    public Guid? OrderId { get; set; }
    public Guid? MerchantId { get; set; }

    // The other participant's details
    public Guid OtherParticipantUserId { get; set; }
    public string OtherParticipantName { get; set; } = string.Empty;

    // Unread messages count for the logged-in user
    public int UnreadCount { get; set; }

    // Last message preview
    public string? LastMessageContent { get; set; }
    public DateTime? LastMessageAt { get; set; }
}

// Individual chat message (Chat room view)
public class ChatMessageResponseDto
{
    public Guid MessageId { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderUserId { get; set; }
    public string SenderName { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    // Delivery Status: Sent, Delivered, Read (WhatsApp Ticks)
    public string DeliveryStatus { get; set; } = "Sent";

    public DateTime SentAt { get; set; }
    public DateTime? ReadAt { get; set; }

    public bool IsEdited { get; set; }
    public bool IsDeletedForEveryone { get; set; }
    public bool IsMine { get; set; } // True if sent by currently authenticated user

    // Attachments
    public string? AttachmentUrl { get; set; }
    public string? AttachmentFileName { get; set; }
    public string? AttachmentType { get; set; }
    public long? AttachmentSizeBytes { get; set; }
}