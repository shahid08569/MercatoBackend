using MercatoApplication.Common;
using MercatoApplication.DTOs.Order;
using MercatoApplication.Interfaces;
using MercatoDomain.Entities;
using MercatoDomain.Enums;
using MercatoInfrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace MercatoInfrastructure.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;
    public OrderService(AppDbContext db)
    {
        _db = db;
    }

    // Executes atomic checkout with multi-vendor order splitting
    public async Task<ApiResponse<OrderResponseDto>> CheckoutAsync(Guid userId, CheckoutRequestDto request)
    {
        // 1. Idempotency Check: prevent duplicate order creation on network retries
        var existingOrder = await _db.Orders
            .Include(o => o.OrderAddresses)
            .Include(o => o.SubOrders)
                .ThenInclude(so => so.OrderItems)
            .Include(o => o.SubOrders)
                .ThenInclude(so => so.Merchant)
            .FirstOrDefaultAsync(o => o.UserId == userId && o.IdempotencyKey == request.IdempotencyKey);

        if (existingOrder != null)
            return ApiResponse<OrderResponseDto>.SuccessResponse(existingOrder.ToDto());

        // 2. Fetch Customer Cart with variants, products, and merchants
        var cart = await _db.Carts
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.ProductVariant)
                    .ThenInclude(pv => pv.Product)
                        .ThenInclude(p => p.Merchant)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null || !cart.CartItems.Any())
            return ApiResponse<OrderResponseDto>.FailureResponse("Your shopping cart is empty.");

        // 3. Stock Availability Verification
        foreach (var item in cart.CartItems)
        {
            if (item.Quantity > item.ProductVariant.StockQuantity)
            {
                return ApiResponse<OrderResponseDto>.FailureResponse(
                    $"Insufficient stock for '{item.ProductVariant.Product.Name}'. Available: {item.ProductVariant.StockQuantity}, Requested: {item.Quantity}");
            }
        }

        // Check if a valid affiliate referral code was provided
        Affiliate? referralAffiliate = null;
        if (!string.IsNullOrWhiteSpace(request.ReferralCode))
        {
            referralAffiliate = await _db.Affiliates
                .FirstOrDefaultAsync(a => a.AffiliateCode == request.ReferralCode.Trim().ToUpper() && a.IsActive);
        }

        // 4. Begin Database Transaction for Atomic Splitting Execution
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var merchantGroups = cart.CartItems
                .GroupBy(ci => ci.ProductVariant.Product.MerchantId)
                .ToList();

            var masterOrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
            var totalSubtotal = cart.CartItems.Sum(ci => (ci.ProductVariant.Product.Price + ci.ProductVariant.PriceModifier) * ci.Quantity);

            // Standard marketplace rule: $10 shipping cost per distinct merchant parcel
            var shippingCostPerMerchant = 10.00m;
            var totalShipping = merchantGroups.Count * shippingCostPerMerchant;
            var totalAmount = totalSubtotal + totalShipping;

            // A. Create Master Order (Customer-facing transaction)
            var masterOrder = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = masterOrderNumber,
                IdempotencyKey = request.IdempotencyKey,
                UserId = userId,
                Status = OrderStatus.PendingPayment,
                Subtotal = totalSubtotal,
                ShippingCost = totalShipping,
                TotalAmount = totalAmount,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Attach delivery address
            masterOrder.OrderAddresses.Add(new OrderAddress
            {
                Id = Guid.NewGuid(),
                OrderId = masterOrder.Id,
                Type = OrderAddressType.Shipping,
                FullName = request.ShippingAddress.FullName,
                Phone = request.ShippingAddress.Phone,
                Street = request.ShippingAddress.Street,
                City = request.ShippingAddress.City,
                State = request.ShippingAddress.State,
                PostalCode = request.ShippingAddress.PostalCode,
                Country = request.ShippingAddress.Country
            });

            _db.Orders.Add(masterOrder);

            // B. Create Sub-Orders for each Merchant
            int subIndex = 1;
            foreach (var group in merchantGroups)
            {
                var merchantId = group.Key;
                var groupSubtotal = group.Sum(ci => (ci.ProductVariant.Product.Price + ci.ProductVariant.PriceModifier) * ci.Quantity);
                var subOrderNumber = $"{masterOrderNumber}-S{subIndex++}";

                var subOrder = new Order
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = subOrderNumber,
                    IdempotencyKey = $"{request.IdempotencyKey}-SUB-{subOrderNumber}",
                    UserId = userId,
                    ParentOrderId = masterOrder.Id,
                    MerchantId = merchantId,
                    Status = OrderStatus.Processing,
                    Subtotal = groupSubtotal,
                    ShippingCost = shippingCostPerMerchant,
                    TotalAmount = groupSubtotal + shippingCostPerMerchant,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                foreach (var ci in group)
                {
                    var unitPrice = ci.ProductVariant.Product.Price + ci.ProductVariant.PriceModifier;

                    var orderItem = new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = subOrder.Id,
                        ProductVariantId = ci.ProductVariantId,
                        MerchantId = merchantId,
                        ProductNameSnapshot = ci.ProductVariant.Product.Name,
                        SkuSnapshot = ci.ProductVariant.SKU,
                        UnitPriceAtPurchase = unitPrice,
                        Quantity = ci.Quantity
                    };

                    subOrder.OrderItems.Add(orderItem);

                    // Automated Affiliate Commission Attribution
                    if (referralAffiliate != null && merchantId.HasValue)
                    {
                        var product = ci.ProductVariant.Product;
                        var commissionRate = product.CustomAffiliateCommissionRate
                                          ?? product.Merchant?.DefaultAffiliateCommissionRate
                                          ?? 5.00m;

                        var lineTotal = unitPrice * ci.Quantity;
                        var commissionAmount = (lineTotal * commissionRate) / 100m;

                        var commissionRecord = new AffiliateCommissionRecord
                        {
                            Id = Guid.NewGuid(),
                            OrderId = subOrder.Id,
                            MerchantId = merchantId.Value,
                            AffiliateId = referralAffiliate.Id,
                            SaleAmount = lineTotal,
                            CommissionPercentage = commissionRate,
                            CommissionAmount = commissionAmount,
                            Status = AffiliateCommissionStatus.PendingDelivery,
                            CreatedAt = DateTime.UtcNow
                        };

                        _db.AffiliateCommissionRecords.Add(commissionRecord);
                    }

                    // Deduct stock from available inventory
                    ci.ProductVariant.StockQuantity -= ci.Quantity;
                }

                masterOrder.SubOrders.Add(subOrder);
                _db.Orders.Add(subOrder);
            }

            // C. Clear Customer Cart
            _db.CartItems.RemoveRange(cart.CartItems);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return ApiResponse<OrderResponseDto>.SuccessResponse(masterOrder.ToDto());
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ApiResponse<OrderResponseDto>.FailureResponse($"Failed to place order: {ex.Message}");
        }
    }

    // Retrieves order details by ID
    public async Task<ApiResponse<OrderResponseDto>> GetOrderByIdAsync(Guid userId, Guid orderId)
    {
        var order = await _db.Orders
            .Include(o => o.OrderAddresses)
            .Include(o => o.SubOrders)
                .ThenInclude(so => so.OrderItems)
            .Include(o => o.SubOrders)
                .ThenInclude(so => so.Merchant)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId && o.ParentOrderId == null);

        if (order == null)
            return ApiResponse<OrderResponseDto>.FailureResponse("Order not found.");

        return ApiResponse<OrderResponseDto>.SuccessResponse(order.ToDto());
    }

    // Retrieves all past master orders for the customer with pagination
    public async Task<ApiResponse<PagedResult<OrderResponseDto>>> GetMyOrdersAsync(Guid userId, int page = 1, int pageSize = 10)
    {
        var query = _db.Orders
            .Include(o => o.OrderAddresses)
            .Include(o => o.SubOrders)
                .ThenInclude(so => so.OrderItems)
            .Include(o => o.SubOrders)
                .ThenInclude(so => so.Merchant)
            .Where(o => o.UserId == userId && o.ParentOrderId == null)
            .OrderByDescending(o => o.CreatedAt);

        var totalCount = await query.CountAsync();
        var orders = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var dtos = orders.Select(o => o.ToDto()).ToList();
        return ApiResponse<PagedResult<OrderResponseDto>>.SuccessResponse(
            new PagedResult<OrderResponseDto>(dtos, totalCount, page, pageSize));
    }

    // Retrieves merchant's Sub-Orders for fulfillment
    public async Task<ApiResponse<PagedResult<SubOrderResponseDto>>> GetMerchantOrdersAsync(Guid merchantId, int page = 1, int pageSize = 10)
    {
        var query = _db.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.Merchant)
            .Where(o => o.MerchantId == merchantId && o.ParentOrderId != null)
            .OrderByDescending(o => o.CreatedAt);

        var totalCount = await query.CountAsync();
        var subOrders = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var dtos = subOrders.Select(so => so.ToSubOrderDto()).ToList();
        return ApiResponse<PagedResult<SubOrderResponseDto>>.SuccessResponse(
            new PagedResult<SubOrderResponseDto>(dtos, totalCount, page, pageSize));
    }

    // Merchant updates fulfillment status (e.g. Shipped, Delivered) and tracking courier
    public async Task<ApiResponse<SubOrderResponseDto>> UpdateSubOrderStatusAsync(
        Guid merchantId, Guid subOrderId, string newStatus, string? trackingNumber, string? courier)
    {
        var subOrder = await _db.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.Merchant)
            .FirstOrDefaultAsync(o => o.Id == subOrderId && o.MerchantId == merchantId && o.ParentOrderId != null);

        if (subOrder == null)
            return ApiResponse<SubOrderResponseDto>.FailureResponse("Sub-order not found or unauthorized.");

        if (Enum.TryParse<OrderStatus>(newStatus, true, out var parsedStatus))
        {
            subOrder.Status = parsedStatus;
        }

        if (!string.IsNullOrWhiteSpace(trackingNumber))
            subOrder.TrackingNumber = trackingNumber;

        if (!string.IsNullOrWhiteSpace(courier))
            subOrder.ShippingCourier = courier;

        subOrder.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return ApiResponse<SubOrderResponseDto>.SuccessResponse(subOrder.ToSubOrderDto());
    }
}