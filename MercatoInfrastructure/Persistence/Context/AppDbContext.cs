using MercatoDomain.Entities;
using MercatoDomain.Enums;
using Microsoft.EntityFrameworkCore;
namespace MercatoInfrastructure.Persistence.Context;

// This class is the "bridge" between EF Core and SQL Server.
// Every DbSet<T> below represents one database table.
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Entities 
    //Identity 
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>(); 
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>(); 
    public DbSet<Address> Addresses => Set<Address>(); 
    // Catalog 
    public DbSet<Brand> Brands => Set<Brand>(); 
    public DbSet<Category> Categories => Set<Category>(); 
    public DbSet<Product> Products => Set<Product>(); 
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>(); 
    public DbSet<MercatoDomain.Entities.VariantAttribute> VariantAttributes => Set<MercatoDomain.Entities.VariantAttribute>(); 
    public DbSet<VariantAttributeValue> VariantAttributeValues => Set<VariantAttributeValue>(); 
    public DbSet<ProductImage> ProductImages => Set<ProductImage>(); 
    public DbSet<InventoryAdjustment> InventoryAdjustments => Set<InventoryAdjustment>(); 
    // Shopping 
    public DbSet<Cart> Carts => Set<Cart>(); 
    public DbSet<CartItem> CartItems => Set<CartItem>(); 
    public DbSet<Wishlist> Wishlists => Set<Wishlist>(); 
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    // Checkout / Orders 
    public DbSet<CheckoutSession> CheckoutSessions => Set<CheckoutSession>(); 
    public DbSet<Order> Orders => Set<Order>(); 
    public DbSet<OrderItem> OrderItems => Set<OrderItem>(); 
    public DbSet<OrderAddress> OrderAddresses => Set<OrderAddress>(); 
    public DbSet<StockReservation> StockReservations => Set<StockReservation>(); 
    public DbSet<Coupon> Coupons => Set<Coupon>(); 
    public DbSet<CouponUsage> CouponUsages => Set<CouponUsage>();
    // Payments 
    public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>(); 
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    // Engagement / Ops 
    public DbSet<Review> Reviews => Set<Review>(); 
    public DbSet<Notification> Notifications => Set<Notification>(); 
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // EF Core automatically calls this method when it needs to build the database model.
    // Instead of manually registering each Configuration class one by one,
    // this line scans the whole project and applies every IEntityTypeConfiguration<T>
    // file it finds (RoleConfiguration, UserConfiguration, AddressConfiguration, etc.)
    protected override void OnModelCreating(ModelBuilder modelBuilder) 
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
} 
