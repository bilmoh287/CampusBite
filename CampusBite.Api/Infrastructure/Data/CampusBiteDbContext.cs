using CampusBite.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CampusBite.Api.Infrastructure.Data;

public class CampusBiteDbContext : DbContext
{
    public CampusBiteDbContext(DbContextOptions<CampusBiteDbContext> options)
        : base(options)
    {
    }

    public DbSet<University> Universities => Set<University>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Cafe> Cafes => Set<Cafe>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<ContractPlan> ContractPlans => Set<ContractPlan>();
    public DbSet<StudentContract> StudentContracts => Set<StudentContract>();
    public DbSet<ContractUsageLog> ContractUsageLogs => Set<ContractUsageLog>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ====================================================================
        // 1. UNIVERSITY CONFIGURATION
        // ====================================================================
        modelBuilder.Entity<University>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Name).IsRequired().HasMaxLength(100);
            entity.Property(u => u.CampusName).HasMaxLength(100);
            entity.Property(u => u.City).IsRequired().HasMaxLength(100);

            entity.HasMany(u => u.Users)
                  .WithOne(usr => usr.University)
                  .HasForeignKey(usr => usr.UniversityId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(u => u.Cafes)
                  .WithOne(c => c.University)
                  .HasForeignKey(c => c.UniversityId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ====================================================================
        // 2. USER CONFIGURATION
        // ====================================================================
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(150);
            entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(u => u.PhoneNumber).HasMaxLength(20);
            entity.Property(u => u.Role).HasConversion<string>().HasMaxLength(30);

            entity.HasMany(u => u.OwnedCafes)
                  .WithOne(c => c.Owner)
                  .HasForeignKey(c => c.OwnerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(u => u.CustomerOrders)
                  .WithOne(o => o.Customer)
                  .HasForeignKey(o => o.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(u => u.CourierOrders)
                  .WithOne(o => o.Courier)
                  .HasForeignKey(o => o.CourierId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(u => u.StudentContracts)
                  .WithOne(sc => sc.Student)
                  .HasForeignKey(sc => sc.StudentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ====================================================================
        // 3. CAFE CONFIGURATION
        // ====================================================================
        modelBuilder.Entity<Cafe>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Location).IsRequired().HasMaxLength(150);
            entity.Property(c => c.LogoUrl).HasMaxLength(255);
            entity.Property(c => c.DeliveryFee).HasPrecision(10, 2);
            entity.Property(c => c.MinimumOrderAmount).HasPrecision(10, 2);

            entity.HasMany(c => c.Categories)
                  .WithOne(cat => cat.Cafe)
                  .HasForeignKey(cat => cat.CafeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.Products)
                  .WithOne(p => p.Cafe)
                  .HasForeignKey(p => p.CafeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.Orders)
                  .WithOne(o => o.Cafe)
                  .HasForeignKey(o => o.CafeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(c => c.ContractPlans)
                  .WithOne(cp => cp.Cafe)
                  .HasForeignKey(cp => cp.CafeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.Expenses)
                  .WithOne(e => e.Cafe)
                  .HasForeignKey(e => e.CafeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ====================================================================
        // 4. CATEGORY CONFIGURATION
        // ====================================================================
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(cat => cat.Id);
            entity.Property(cat => cat.Name).IsRequired().HasMaxLength(50);

            entity.HasMany(cat => cat.Products)
                  .WithOne(p => p.Category)
                  .HasForeignKey(p => p.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ====================================================================
        // 5. PRODUCT CONFIGURATION
        // ====================================================================
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Price).HasPrecision(10, 2);
            entity.Property(p => p.ImageUrl).HasMaxLength(255);

            entity.HasMany(p => p.OrderItems)
                  .WithOne(oi => oi.Product)
                  .HasForeignKey(oi => oi.ProductId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ====================================================================
        // 6. ORDER CONFIGURATION
        // ====================================================================
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.HasIndex(o => o.OrderNumber).IsUnique();
            entity.Property(o => o.OrderNumber).IsRequired().HasMaxLength(20);
            entity.Property(o => o.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(o => o.PaymentMethod).HasConversion<string>().HasMaxLength(30);
            entity.Property(o => o.Subtotal).HasPrecision(10, 2);
            entity.Property(o => o.DeliveryFee).HasPrecision(10, 2);
            entity.Property(o => o.DiscountAmount).HasPrecision(10, 2);
            entity.Property(o => o.TotalAmount).HasPrecision(10, 2);
            entity.Property(o => o.DeliveryLocation).IsRequired().HasMaxLength(200);

            entity.HasMany(o => o.OrderItems)
                  .WithOne(oi => oi.Order)
                  .HasForeignKey(oi => oi.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(o => o.ContractUsageLogs)
                  .WithOne(cul => cul.Order)
                  .HasForeignKey(cul => cul.OrderId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ====================================================================
        // 7. ORDER ITEM CONFIGURATION (Snapshot Pattern)
        // ====================================================================
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(oi => oi.Id);
            entity.Property(oi => oi.ProductName).IsRequired().HasMaxLength(100);
            entity.Property(oi => oi.UnitPrice).HasPrecision(10, 2);
            entity.Property(oi => oi.ItemTotal).HasPrecision(10, 2);
        });

        // ====================================================================
        // 8. CONTRACT PLAN CONFIGURATION
        // ====================================================================
        modelBuilder.Entity<ContractPlan>(entity =>
        {
            entity.HasKey(cp => cp.Id);
            entity.Property(cp => cp.Name).IsRequired().HasMaxLength(100);
            entity.Property(cp => cp.Price).HasPrecision(10, 2);

            entity.HasMany(cp => cp.StudentContracts)
                  .WithOne(sc => sc.ContractPlan)
                  .HasForeignKey(sc => sc.ContractPlanId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ====================================================================
        // 9. STUDENT CONTRACT CONFIGURATION
        // ====================================================================
        modelBuilder.Entity<StudentContract>(entity =>
        {
            entity.HasKey(sc => sc.Id);
            entity.Property(sc => sc.Status).HasConversion<string>().HasMaxLength(30);

            entity.HasMany(sc => sc.UsageLogs)
                  .WithOne(cul => cul.StudentContract)
                  .HasForeignKey(cul => cul.StudentContractId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ====================================================================
        // 10. CONTRACT USAGE LOG CONFIGURATION
        // ====================================================================
        modelBuilder.Entity<ContractUsageLog>(entity =>
        {
            entity.HasKey(cul => cul.Id);
        });

        // ====================================================================
        // 11. EXPENSE CONFIGURATION
        // ====================================================================
        modelBuilder.Entity<Expense>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Category).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Amount).HasPrecision(10, 2);
        });

        // ====================================================================
        // 12. REFRESH TOKEN CONFIGURATION
        // ====================================================================
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(rt => rt.Id);
            entity.HasIndex(rt => rt.Token).IsUnique();
            entity.Property(rt => rt.Token).IsRequired().HasMaxLength(255);

            entity.HasOne(rt => rt.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(rt => rt.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
