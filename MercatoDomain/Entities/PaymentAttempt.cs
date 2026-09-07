using MercatoDomain.Enums;
namespace MercatoDomain.Entities;
public class PaymentAttempt
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public PaymentProvider Provider { get; set; }
    public string? ProviderIntentId { get; set; }
    public PaymentAttemptStatus Status { get; set; } = PaymentAttemptStatus.Created;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; 
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Order Order { get; set; } = null!;
    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
}