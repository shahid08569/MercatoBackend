using MercatoDomain.Enums;

namespace MercatoDomain.Entities;
public class StockReservation
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductVariantId { get; set; }
    public int Quantity { get; set; }
    public StockReservationStatus Status { get; set; } = StockReservationStatus.Reserved;
    public DateTime ReservedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Order Order { get; set; } = null!;
    public ProductVariant ProductVariant { get; set; } = null!;
}