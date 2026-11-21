using MercatoApplication.Common;
using MercatoApplication.DTOs.Order;

namespace MercatoApplication.Interfaces;

public interface IOrderService
{
    // Executes atomic checkout: validates cart, creates Master Order,
    // splits into merchant Sub-Orders, reduces stock, and clears the cart.
    Task<ApiResponse<OrderResponseDto>> CheckoutAsync(Guid userId, CheckoutRequestDto request);

    // Retrieves order details by ID for customer
    Task<ApiResponse<OrderResponseDto>> GetOrderByIdAsync(Guid userId, Guid orderId);

    // Retrieves paginated list of orders placed by the customer
    Task<ApiResponse<PagedResult<OrderResponseDto>>> GetMyOrdersAsync(Guid userId, int page = 1, int pageSize = 10);

    // Retrieves merchant-specific Sub-Orders for order fulfillment
    Task<ApiResponse<PagedResult<SubOrderResponseDto>>> GetMerchantOrdersAsync(Guid merchantId, int page = 1, int pageSize = 10);

    // Allows merchant to mark sub-order as Shipped/Delivered and attach tracking number
    Task<ApiResponse<SubOrderResponseDto>> UpdateSubOrderStatusAsync(Guid merchantId, Guid subOrderId, string newStatus, string? trackingNumber, string? courier);
}