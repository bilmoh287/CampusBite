using CampusBite.Data;
using CampusBite.Services.Implementations;
using CampusBite.Services.Interfaces;
using CampusBite.ViewModels;
using CampusBite.Views;
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

        // Register Database Context
        builder.Services.AddDbContext<AppDbContext>();

        // Register Services
        builder.Services.AddScoped<ICafeService, CafeService>();
        builder.Services.AddScoped<IProductService, ProductService>();
        builder.Services.AddScoped<IOrderService, OrderService>();
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

        // Register Views
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<CafeListPage>();
        builder.Services.AddTransient<CafeMenuPage>();
        builder.Services.AddTransient<FoodDetailsPage>();
        builder.Services.AddTransient<CartPage>();
        builder.Services.AddTransient<CheckoutPage>();
        builder.Services.AddTransient<OrderHistoryPage>();
        builder.Services.AddTransient<OrderDetailsPage>();

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
