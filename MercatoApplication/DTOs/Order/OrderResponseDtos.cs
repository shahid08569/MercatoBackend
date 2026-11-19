namespace MercatoApplication.DTOs.Order;

// Customer-facing Master Order response
public class OrderResponseDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    // Master Financials
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; }
    public OrderAddressDto ShippingAddress { get; set; } = new();

    // Individual Merchant Packages (Sub-Orders)
    public List<SubOrderResponseDto> SubOrders { get; set; } = new();
}

// Merchant-specific Sub-Order details
public class SubOrderResponseDto
{
    public Guid SubOrderId { get; set; }
    public string SubOrderNumber { get; set; } = string.Empty;
    public Guid MerchantId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    // Sub-order Financials
    public decimal Subtotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal TotalAmount { get; set; }

    // Courier tracking info for this specific store's package
    public string? TrackingNumber { get; set; }
    public string? ShippingCourier { get; set; }

    // Products in this merchant's package
    public List<OrderItemResponseDto> Items { get; set; } = new();
}

// Line item breakdown inside a package
public class OrderItemResponseDto
{
    public Guid OrderItemId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice => UnitPrice * Quantity;
}