namespace MercatoDomain.Entities;

using MercatoDomain.Enums;

public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Type of interaction boundary (e.g. SuperAdminToMerchant, MerchantToCustomer)
    public ConversationType Type { get; set; }

    // Optional topic or title (e.g. "Inquiry for Order #ORD-1002" or "Commission Discussion")
    public string? Title { get; set; }

    // Optional context references to enforce business relation boundaries
    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }

    public Guid? MerchantId { get; set; }
    public Merchant? Merchant { get; set; }

    public Guid? PartnerId { get; set; }
    public Partner? Partner { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastMessageAt { get; set; }

    // Navigation properties: participants and messages
    public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}           