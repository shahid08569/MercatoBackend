using MercatoDomain.Enums;
namespace MercatoDomain.Entities;
    public class InventoryAdjustment
    {
        public Guid Id { get; set; }
        public Guid ProductVariantId { get; set; }
        public int ChangeAmount { get; set; }
        public InventoryAdjustmentReason Reason { get; set; }
        public InventoryReferenceType ReferenceType { get; set; }
        public Guid? ReferenceId { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; 
        public ProductVariant ProductVariant { get; set; } = null!;
        public User? CreatedByUser { get; set; }
    }
