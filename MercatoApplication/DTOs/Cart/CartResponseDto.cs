namespace MercatoApplication.DTOs.Cart;

// Individual item line inside the cart
public class CartItemDto
{
    public Guid CartItemId { get; set; }
    public Guid ProductVariantId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
    public int AvailableStock { get; set; }
}

// Items grouped per store/merchant for multi-vendor checkout clarity
public class StoreCartGroupDto
{
    public Guid? MerchantId { get; set; }
    public string StoreName { get; set; } = "Direct / Independent Seller";
    public List<CartItemDto> Items { get; set; } = new();
    public decimal StoreSubtotal => Items.Sum(i => i.LineTotal);
}

// Complete cart response returned to the frontend
public class CartResponseDto
{
    public Guid CartId { get; set; }
    public List<StoreCartGroupDto> StoreGroups { get; set; } = new();
    public int TotalItemsCount => StoreGroups.Sum(g => g.Items.Sum(i => i.Quantity));
    public decimal GrandTotal => StoreGroups.Sum(g => g.StoreSubtotal);
}