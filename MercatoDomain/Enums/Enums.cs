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
public enum MerchantVerificationStatus { Pending, Approved, Suspended, Rejected }
public enum BusinessModelType { B2C, B2B, Both }
public enum AffiliateCommissionStatus
{
    PendingDelivery,
    PendingVerification,
    TransferredByMerchant,
    ForceTransferredBySuperAdmin,
    Disputed,
    Cancelled
}
public enum SubscriptionStatus
{
    Active,
    Expired,
    Canceled,
    GracePeriod
}
public enum ProductListingType { B2C, B2B, C2C }
public enum ProductCondition { BrandNew, LikeNew, Good, Fair }

public enum WalletOwnerType { Platform, PlatformPartner, Merchant, Affiliate, Customer }

public enum WalletTransactionType
{
    CreditSale,
    DebitPlatformCommission,
    CreditAffiliateCommission,
    DebitAffiliatePayout,
    ForceTransferByAdmin,
    SubscriptionFeePayment,
    PayoutWithdrawal,
    RefundAdjustment
}
public enum AffiliateType
{
    SellerAffiliate,
    PartnerAffiliate
}

public enum ConversationType
{
    SuperAdminToPartner,
    SuperAdminToMerchant,
    SuperAdminToAffiliate,
    PartnerToMerchant,
    PartnerToCustomer,
    MerchantToAffiliate,
    MerchantToCustomer,
    AffiliateToCustomer
}
public enum MessageDeliveryStatus
{
    Sent,      // Single tick
    Delivered, // Double gray tick
    Read       // Double blue tick
}