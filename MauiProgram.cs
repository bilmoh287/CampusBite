using CampusBite.Data;
using CampusBite.Services.Implementations;
using CampusBite.Services.Interfaces;
using CampusBite.ViewModels;
using CampusBite.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CampusBite;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register Database Context with Transient lifetime for thread safety in MAUI
        builder.Services.AddDbContext<AppDbContext>(ServiceLifetime.Transient, ServiceLifetime.Transient);

        // Register Services (Transient to ensure fresh DbContext instances per operation)
        builder.Services.AddTransient<ICafeService, CafeService>();
        builder.Services.AddTransient<IProductService, ProductService>();
        builder.Services.AddTransient<IOrderService, OrderService>();
        builder.Services.AddTransient<ICafeAdminService, CafeAdminService>();
        builder.Services.AddSingleton<ICartService, CartService>(); // Singleton state manager!

        // Register ViewModels
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<CafeListViewModel>();
        builder.Services.AddTransient<CafeMenuViewModel>();
        builder.Services.AddTransient<FoodDetailsViewModel>();
        builder.Services.AddTransient<CartViewModel>();
        builder.Services.AddTransient<CheckoutViewModel>();
        builder.Services.AddTransient<OrderHistoryViewModel>();
        builder.Services.AddTransient<OrderDetailsViewModel>();
        builder.Services.AddTransient<CafeAdminViewModel>();

        // Register Views
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<CafeListPage>();
        builder.Services.AddTransient<CafeMenuPage>();
        builder.Services.AddTransient<FoodDetailsPage>();
        builder.Services.AddTransient<CartPage>();
        builder.Services.AddTransient<CheckoutPage>();
        builder.Services.AddTransient<OrderHistoryPage>();
        builder.Services.AddTransient<OrderDetailsPage>();
        builder.Services.AddTransient<CafeAdminPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // Initialize SQLite database and seed initial campus data on startup
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.EnsureCreated();
        }

        return app;
    }
}
