using CampusBite.Data;
using CampusBite.Services.Implementations;
using CampusBite.Services.Interfaces;
using CampusBite.Services.Network;
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

        // 1. Register Network & Token Infrastructure
        builder.Services.AddSingleton<IAuthTokenStorage, AuthTokenStorage>();
        builder.Services.AddTransient<AuthHeaderHandler>();
        builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
        {
            client.BaseAddress = new Uri(ApiConstants.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddHttpMessageHandler<AuthHeaderHandler>();

        // Register Database Context with Transient lifetime for thread safety in MAUI
        builder.Services.AddDbContext<AppDbContext>(ServiceLifetime.Transient, ServiceLifetime.Transient);

        // Register Services
        builder.Services.AddTransient<IAuthService, AuthService>();
        builder.Services.AddTransient<ICafeService, CafeService>();
        builder.Services.AddTransient<IProductService, ProductService>();
        builder.Services.AddTransient<IOrderService, OrderService>();
        builder.Services.AddTransient<ICafeAdminService, CafeAdminService>();
        builder.Services.AddTransient<IDeliveryService, DeliveryService>();
        builder.Services.AddSingleton<ICartService, CartService>(); // Singleton state manager!

        // Register ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<CafeListViewModel>();
        builder.Services.AddTransient<CafeMenuViewModel>();
        builder.Services.AddTransient<FoodDetailsViewModel>();
        builder.Services.AddTransient<CartViewModel>();
        builder.Services.AddTransient<CheckoutViewModel>();
        builder.Services.AddTransient<OrderHistoryViewModel>();
        builder.Services.AddTransient<OrderDetailsViewModel>();
        builder.Services.AddTransient<CafeAdminViewModel>();
        builder.Services.AddTransient<DeliveryRunnerViewModel>();

        // Register Views & App Navigation
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<CafeListPage>();
        builder.Services.AddTransient<CafeMenuPage>();
        builder.Services.AddTransient<FoodDetailsPage>();
        builder.Services.AddTransient<CartPage>();
        builder.Services.AddTransient<CheckoutPage>();
        builder.Services.AddTransient<OrderHistoryPage>();
        builder.Services.AddTransient<OrderDetailsPage>();
        builder.Services.AddTransient<CafeAdminPage>();
        builder.Services.AddTransient<DeliveryRunnerPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
