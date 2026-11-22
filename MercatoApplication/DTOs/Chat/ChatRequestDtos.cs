namespace MercatoApplication.DTOs.Chat;

// Request to start or retrieve an existing conversation room
public class CreateConversationRequestDto
{
    // The other user to chat with (e.g. Merchant Owner or Customer)
    public Guid RecipientUserId { get; set; }

    // Optional topic/subject
    public string? Title { get; set; }

    // Optional business context references
    public Guid? OrderId { get; set; }
    public Guid? MerchantId { get; set; }
}

// Request to post a new message inside a conversation
public class SendMessageRequestDto
{
    public string Content { get; set; } = string.Empty;

    // Optional Media / Attachment details
    public string? AttachmentUrl { get; set; }
    public string? AttachmentFileName { get; set; }
    public string? AttachmentType { get; set; } // e.g. "image/png", "application/pdf"
    public long? AttachmentSizeBytes { get; set; }
}

// Request to edit a previously sent message
public class EditMessageRequestDto
{
    public string NewContent { get; set; } = string.Empty;
}