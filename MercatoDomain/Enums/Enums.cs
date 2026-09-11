namespace MercatoDomain.Enums;
public enum OrderStatus { PendingPayment, Confirmed, PaymentFailed, Cancelled, Processing, Shipped, Delivered, Refunded }
public enum OrderAddressType { Shipping, Billing }
public enum CheckoutSessionStatus { Active, Confirmed, Expired }
public enum InventoryAdjustmentReason { Sale, ReservationRelease, Restock, Correction, Return }
public enum InventoryReferenceType { Order, StockReservation, Manual }
public enum StockReservationStatus { Reserved, Committed, Released, Expired }
public enum CouponDiscountType { Percentage, Fixed }
public enum PaymentProvider { Stripe, JazzCash, Easypaisa }
public enum PaymentAttemptStatus { Created, Pending, Succeeded, Failed, Cancelled, Refunded }
public enum PaymentTransactionEventType { Created, Succeeded, Failed, Refunded, ChargebackDisputed }
