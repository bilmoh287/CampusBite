using CampusBite.Services.Interfaces;
using CampusBite.Shared.Enums;
using CampusBite.Views;

namespace CampusBite;

public partial class AppShell : Shell
{
    private readonly IAuthTokenStorage _tokenStorage;
    private readonly IAuthService _authService;
    private bool _hasInitialized;

    public AppShell(IAuthTokenStorage tokenStorage, IAuthService authService)
    {
        InitializeComponent();
        _tokenStorage = tokenStorage;
        _authService = authService;

        Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
        Routing.RegisterRoute(nameof(CafeMenuPage), typeof(CafeMenuPage));
        Routing.RegisterRoute(nameof(FoodDetailsPage), typeof(FoodDetailsPage));
        Routing.RegisterRoute(nameof(CheckoutPage), typeof(CheckoutPage));
        Routing.RegisterRoute(nameof(OrderDetailsPage), typeof(OrderDetailsPage));
        Routing.RegisterRoute(nameof(MealPlansPage), typeof(MealPlansPage));

        Loaded += OnAppShellLoaded;
    }

    private async void OnAppShellLoaded(object? sender, EventArgs e)
    {
        if (_hasInitialized) return;
        _hasInitialized = true;

        await CheckAuthenticationStateAsync();
    }

    public async Task CheckAuthenticationStateAsync()
    {
        try
        {
            var isAuth = await _tokenStorage.IsAuthenticatedAsync();
            if (!isAuth)
            {
                ApplyRoleGating(null);
                await GoToAsync("//LoginPage");
                return;
            }

            var role = await _tokenStorage.GetUserRoleAsync();
            ApplyRoleGating(role);

            switch (role)
            {
                case UserRole.Courier:
                    await GoToAsync("//DeliveryRunnerPage");
                    break;
                case UserRole.CafeOwner:
                case UserRole.CafeStaff:
                    await GoToAsync("//CafeAdminPage");
                    break;
                default:
                    await GoToAsync("//CafeListPage");
                    break;
            }
        }
        catch
        {
            ApplyRoleGating(null);
            await GoToAsync("//LoginPage");
        }
    }

    public void ApplyRoleGating(UserRole? role)
    {
        if (role == null)
        {
            CafesTab.IsVisible = false;
            CartTab.IsVisible = false;
            PassesTab.IsVisible = false;
            OrdersTab.IsVisible = false;
            CourierTab.IsVisible = false;
            AdminTab.IsVisible = false;
            return;
        }

        switch (role)
        {
            case UserRole.Student:
            case UserRole.Customer:
                CafesTab.IsVisible = true;
                CartTab.IsVisible = true;
                PassesTab.IsVisible = true;
                OrdersTab.IsVisible = true;
                CourierTab.IsVisible = false;
                AdminTab.IsVisible = false;
                break;

            case UserRole.Courier:
                CafesTab.IsVisible = false;
                CartTab.IsVisible = false;
                PassesTab.IsVisible = false;
                OrdersTab.IsVisible = false;
                CourierTab.IsVisible = true;
                AdminTab.IsVisible = false;
                break;

            case UserRole.CafeOwner:
            case UserRole.CafeStaff:
                CafesTab.IsVisible = false;
                CartTab.IsVisible = false;
                PassesTab.IsVisible = false;
                OrdersTab.IsVisible = false;
                CourierTab.IsVisible = false;
                AdminTab.IsVisible = true;
                break;

            case UserRole.Admin:
                CafesTab.IsVisible = true;
                CartTab.IsVisible = true;
                PassesTab.IsVisible = true;
                OrdersTab.IsVisible = true;
                CourierTab.IsVisible = true;
                AdminTab.IsVisible = true;
                break;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _authService.LogoutAsync();
        }
        finally
        {
            ApplyRoleGating(null);
            await GoToAsync("//LoginPage");
        }
    }
}
