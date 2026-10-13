using CampusBite.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusBite.Data;

public class AppDbContext : DbContext
{
    public DbSet<Cafe> Cafes => Set<Cafe>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "campusbite.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Decimal precision for currency columns
        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<OrderItem>()
            .Property(oi => oi.UnitPrice)
            .HasColumnType("decimal(18,2)");

        // Configure Relationships
        modelBuilder.Entity<Cafe>()
            .HasMany(c => c.Products)
            .WithOne(p => p.Cafe)
            .HasForeignKey(p => p.CafeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Cafe>()
            .HasMany(c => c.Orders)
            .WithOne(o => o.Cafe)
            .HasForeignKey(o => o.CafeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Order>()
            .HasMany(o => o.OrderItems)
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed Sample Campus Cafés & Food Items
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cafe>().HasData(
            new Cafe
            {
                Id = 1,
                Name = "Central Library Grind",
                Description = "Artisanal coffee and fresh pastries inside the main campus library.",
                Location = "Library 1st Floor",
                IsOpen = true,
                ImageUrl = "dotnet_bot.png",
                Announcement = "☕ Midterm Special: 20% off all large lattes between 2 PM and 5 PM!"
            },
            new Cafe
            {
                Id = 2,
                Name = "Tech Hub Lounge",
                Description = "Fast-grab sandwiches, energy smoothies, and espresso for engineers.",
                Location = "Engineering Building Block C",
                IsOpen = true,
                ImageUrl = "dotnet_bot.png",
                Announcement = "⚡ Fresh sourdough paninis baked daily at 11 AM."
            },
            new Cafe
            {
                Id = 3,
                Name = "Student Union Diner",
                Description = "Budget-friendly hot meals, burgers, and student combo deals.",
                Location = "Student Union Center",
                IsOpen = false,
                ImageUrl = "dotnet_bot.png",
                Announcement = null
            }
        );

        modelBuilder.Entity<Product>().HasData(
            // Central Library Grind Items (CafeId = 1)
            new Product
            {
                Id = 1,
                CafeId = 1,
                Name = "Caramel Macchiato",
                Description = "Rich espresso with steamed milk and vanilla syrup drizzled with caramel.",
                Price = 3.50m,
                Category = "Beverages",
                IsAvailable = true,
                ImageUrl = "dotnet_bot.png"
            },
            new Product
            {
                Id = 2,
                CafeId = 1,
                Name = "Chocolate Croissant",
                Description = "Flaky buttery pastry filled with rich dark chocolate.",
                Price = 2.75m,
                Category = "Pastries",
                IsAvailable = true,
                ImageUrl = "dotnet_bot.png"
            },
            new Product
            {
                Id = 3,
                CafeId = 1,
                Name = "Cold Brew Coffee",
                Description = "Slow-steeped 16-hour cold brew over ice.",
                Price = 3.00m,
                Category = "Beverages",
                IsAvailable = true,
                ImageUrl = "dotnet_bot.png"
            },

            // Tech Hub Lounge Items (CafeId = 2)
            new Product
            {
                Id = 4,
                CafeId = 2,
                Name = "Grilled Chicken Panini",
                Description = "Seasoned chicken breast, mozzarella, and pesto on sourdough.",
                Price = 5.50m,
                Category = "Sandwiches",
                IsAvailable = true,
                ImageUrl = "dotnet_bot.png"
            },
            new Product
            {
                Id = 5,
                CafeId = 2,
                Name = "Matcha Green Tea Latte",
                Description = "Ceremonial grade matcha blended with oat milk.",
                Price = 4.00m,
                Category = "Beverages",
                IsAvailable = true,
                ImageUrl = "dotnet_bot.png"
            },

            // Student Union Diner Items (CafeId = 3)
            new Product
            {
                Id = 6,
                CafeId = 3,
                Name = "Classic Cheeseburger Combo",
                Description = "Beef patty with cheddar, lettuce, tomato, fries, and a soft drink.",
                Price = 6.50m,
                Category = "Combos",
                IsAvailable = false,
                ImageUrl = "dotnet_bot.png"
            }
        );
    }
}
