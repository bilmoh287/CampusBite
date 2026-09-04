using CampusBite.Data;
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

        // Register ViewModels
        builder.Services.AddTransient<MainViewModel>();

        // Register Views
        builder.Services.AddTransient<MainPage>();

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
