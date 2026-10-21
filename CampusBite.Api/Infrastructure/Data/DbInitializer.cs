using CampusBite.Api.Domain.Entities;
using CampusBite.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CampusBite.Api.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(CampusBiteDbContext context)
    {
        // Ensure database is created
        await context.Database.MigrateAsync();

        // 1. Seed Universities
        if (!await context.Universities.AnyAsync())
        {
            var unis = new List<University>
            {
                new() { Name = "Addis Ababa University", CampusName = "4 Kilo Campus", City = "Addis Ababa", IsActive = true },
                new() { Name = "Addis Ababa University", CampusName = "6 Kilo (Main Campus)", City = "Addis Ababa", IsActive = true },
                new() { Name = "Adama Science and Technology University", CampusName = "Main Campus", City = "Adama", IsActive = true }
            };
            await context.Universities.AddRangeAsync(unis);
            await context.SaveChangesAsync();
        }

        var aau4Kilo = await context.Universities.FirstOrDefaultAsync(u => u.CampusName == "4 Kilo Campus");
        int? uniId = aau4Kilo?.Id;

        // 2. Seed Users
        if (!await context.Users.AnyAsync())
        {
            var defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!");

            var users = new List<User>
            {
                new()
                {
                    FullName = "Admin User",
                    Email = "admin@campusbite.com",
                    PasswordHash = defaultPasswordHash,
                    Role = UserRole.Admin,
                    PhoneNumber = "+251911000000",
                    UniversityId = uniId
                },
                new()
                {
                    FullName = "Cafe Manager",
                    Email = "manager@centralcafe.com",
                    PasswordHash = defaultPasswordHash,
                    Role = UserRole.CafeOwner,
                    PhoneNumber = "+251911111111",
                    UniversityId = uniId
                },
                new()
                {
                    FullName = "John Student",
                    Email = "student@aau.edu.et",
                    PasswordHash = defaultPasswordHash,
                    Role = UserRole.Student,
                    PhoneNumber = "+251922222222",
                    UniversityId = uniId
                },
                new()
                {
                    FullName = "Swift Courier",
                    Email = "courier@campusbite.com",
                    PasswordHash = defaultPasswordHash,
                    Role = UserRole.Courier,
                    PhoneNumber = "+251933333333",
                    UniversityId = uniId
                }
            };
            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();
        }

        var managerUser = await context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.CafeOwner);

        // 3. Seed Cafes
        if (!await context.Cafes.AnyAsync() && managerUser != null)
        {
            var centralCafe = new Cafe
            {
                Name = "Central Campus Diner",
                Description = "Fresh hot meals, specialty coffees, and traditional breakfast bowls.",
                Location = "Student Center, Ground Floor",
                LogoUrl = "https://images.unsplash.com/photo-1554118811-1e0d58224f24?w=500",
                DeliveryFee = 15.00m,
                MinimumOrderAmount = 50.00m,
                OwnerId = managerUser.Id,
                UniversityId = uniId,
                IsActive = true
            };
            await context.Cafes.AddAsync(centralCafe);
            await context.SaveChangesAsync();

            // 4. Seed Categories
            var categories = new List<Category>
            {
                new() { CafeId = centralCafe.Id, Name = "Popular Meals", DisplayOrder = 1 },
                new() { CafeId = centralCafe.Id, Name = "Breakfast & Bowls", DisplayOrder = 2 },
                new() { CafeId = centralCafe.Id, Name = "Hot & Cold Drinks", DisplayOrder = 3 }
            };
            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();

            var catPopular = categories[0];
            var catBreakfast = categories[1];
            var catDrinks = categories[2];

            // 5. Seed Products
            var products = new List<Product>
            {
                new()
                {
                    CafeId = centralCafe.Id,
                    CategoryId = catPopular.Id,
                    Name = "Special Tibs with Injera",
                    Description = "Sautéed lean beef chunks seasoned with garlic, onions, and rosemary.",
                    Price = 220.00m,
                    StockQuantity = 25,
                    IsAvailable = true,
                    ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=500"
                },
                new()
                {
                    CafeId = centralCafe.Id,
                    CategoryId = catPopular.Id,
                    Name = "Crispy Chicken Burger",
                    Description = "Golden fried chicken breast fillet with melted cheese and house secret sauce.",
                    Price = 180.00m,
                    StockQuantity = 15,
                    IsAvailable = true,
                    ImageUrl = "https://images.unsplash.com/photo-1568901346375-23c9450c58cd?w=500"
                },
                new()
                {
                    CafeId = centralCafe.Id,
                    CategoryId = catBreakfast.Id,
                    Name = "Chechebsa with Honey",
                    Description = "Torn flatbread braised in spiced clarified butter (kibe) and served with pure honey.",
                    Price = 130.00m,
                    StockQuantity = 20,
                    IsAvailable = true,
                    ImageUrl = "https://images.unsplash.com/photo-1525351484163-7529414344d8?w=500"
                },
                new()
                {
                    CafeId = centralCafe.Id,
                    CategoryId = catDrinks.Id,
                    Name = "Fresh Mango Avocado Juice",
                    Description = "Layered thick tropical smoothie made from fresh local fruits.",
                    Price = 75.00m,
                    StockQuantity = null, // Unlimited/Not tracked
                    IsAvailable = true,
                    ImageUrl = "https://images.unsplash.com/photo-1546173159-315724a31696?w=500"
                }
            };
            await context.Products.AddRangeAsync(products);

            // 6. Seed Contract Plans
            var contractPlans = new List<ContractPlan>
            {
                new()
                {
                    CafeId = centralCafe.Id,
                    Name = "Monthly Standard (30 Meals)",
                    Description = "Includes 30 full lunch or dinner meals valid for 30 days. Perfect for daily dining.",
                    Price = 3200.00m,
                    TotalCredits = 30,
                    ValidityDays = 30,
                    IsActive = true
                },
                new()
                {
                    CafeId = centralCafe.Id,
                    Name = "Full Semester Pass (60 Meals)",
                    Description = "Includes 60 meals with flexible rollover and trusted credit overdraft allowance.",
                    Price = 6000.00m,
                    TotalCredits = 60,
                    ValidityDays = 90,
                    IsActive = true
                }
            };
            await context.ContractPlans.AddRangeAsync(contractPlans);
            await context.SaveChangesAsync();
        }
    }
}
