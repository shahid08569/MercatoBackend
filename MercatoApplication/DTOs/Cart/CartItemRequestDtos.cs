namespace MercatoApplication.DTOs.Cart;

// Data sent by customer when adding an item to the shopping cart
public class AddToCartRequestDto
{
    public Guid ProductVariantId { get; set; }
    public int Quantity { get; set; } = 1;
}

// Data sent when changing quantity (+ or -) in the cart
public class UpdateCartItemQuantityDto
{
    public int Quantity { get; set; }
}