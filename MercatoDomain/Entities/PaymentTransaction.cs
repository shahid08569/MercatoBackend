using MercatoDomain.Enums;
namespace MercatoDomain.Entities;
public class PaymentTransaction
{
    public Guid Id { get; set; }
    public Guid PaymentAttemptId { get; set; }
    public string ProviderReferenceId { get; set; } = string.Empty; 
    public PaymentTransactionEventType EventType { get; set; }
    public string? RawStatus { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public PaymentAttempt PaymentAttempt { get; set; } = null!;
}