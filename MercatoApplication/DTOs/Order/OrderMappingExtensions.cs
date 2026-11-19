using MercatoDomain.Entities;
using MercatoDomain.Enums;

namespace MercatoApplication.DTOs.Order;

public static class OrderMappingExtensions
{
    // Maps Master Order Entity into customer response DTO
    public static OrderResponseDto ToDto(this MercatoDomain.Entities.Order order)
    {
        var shippingAddress = order.OrderAddresses.FirstOrDefault(a => a.Type == OrderAddressType.Shipping);

        return new OrderResponseDto
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status.ToString(),
            Subtotal = order.Subtotal,
            Discount = order.Discount,
            Tax = order.Tax,
            ShippingCost = order.ShippingCost,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt,
            ShippingAddress = shippingAddress != null ? new OrderAddressDto
            {
                FullName = shippingAddress.FullName,
                Phone = shippingAddress.Phone,
                Street = shippingAddress.Street,
                City = shippingAddress.City,
                State = shippingAddress.State,
                PostalCode = shippingAddress.PostalCode,
                Country = shippingAddress.Country
            } : new OrderAddressDto(),
            SubOrders = order.SubOrders.Select(so => so.ToSubOrderDto()).ToList()
        };
    }

    // Maps child Sub-Order Entity into merchant package DTO
    public static SubOrderResponseDto ToSubOrderDto(this MercatoDomain.Entities.Order subOrder)
    {
        return new SubOrderResponseDto
        {
            SubOrderId = subOrder.Id,
            SubOrderNumber = subOrder.OrderNumber,
            MerchantId = subOrder.MerchantId ?? Guid.Empty,
            StoreName = subOrder.Merchant?.StoreName ?? "Independent Merchant",
            Status = subOrder.Status.ToString(),
            Subtotal = subOrder.Subtotal,
            ShippingCost = subOrder.ShippingCost,
            TotalAmount = subOrder.TotalAmount,
            TrackingNumber = subOrder.TrackingNumber,
            ShippingCourier = subOrder.ShippingCourier,
            Items = subOrder.OrderItems.Select(oi => oi.ToDto()).ToList()
        };
    }

    // Maps OrderItem snapshot entity into item DTO
    public static OrderItemResponseDto ToDto(this OrderItem item)
    {
        return new OrderItemResponseDto
        {
            OrderItemId = item.Id,
            ProductVariantId = item.ProductVariantId,
            ProductName = item.ProductNameSnapshot,
            Sku = item.SkuSnapshot,
            UnitPrice = item.UnitPriceAtPurchase,
            Quantity = item.Quantity
        };
    }
}