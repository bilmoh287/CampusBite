using CampusBite.Services.Interfaces;
using CampusBite.Shared.Enums;
using CampusBite.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CampusBite.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    public async Task LoginAsync()
    {
        if (IsBusy) return;

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter both email and password.";
            HasError = true;
            return;
        }

        try
        {
            IsBusy = true;
            HasError = false;
            ErrorMessage = null;

            var response = await _authService.LoginAsync(Email.Trim(), Password);

            if (response != null && response.Success && response.Data != null)
            {
                var role = response.Data.User.Role;

                if (Shell.Current is AppShell appShell)
                {
                    appShell.ApplyRoleGating(role);
                }

                switch (role)
                {
                    case UserRole.Courier:
                        await Shell.Current.GoToAsync("//DeliveryRunnerPage");
                        break;
                    case UserRole.CafeOwner:
                        await Shell.Current.GoToAsync("//CafeAdminPage");
                        break;
                    default:
                        await Shell.Current.GoToAsync("//CafeListPage");
                        break;
                }
            }
            else
            {
                ErrorMessage = response?.Message ?? "Unable to log in. Please verify your credentials.";
                HasError = true;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Connection error: {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task QuickLoginStudentAsync()
    {
        Email = "student@aau.edu.et";
        Password = "Password123!";
        await LoginAsync();
    }

    [RelayCommand]
    public async Task QuickLoginCourierAsync()
    {
        Email = "courier@campusbite.com";
        Password = "Password123!";
        await LoginAsync();
    }

    [RelayCommand]
    public async Task QuickLoginCafeOwnerAsync()
    {
        Email = "manager@centralcafe.com";
        Password = "Password123!";
        await LoginAsync();
    }

    [RelayCommand]
    public async Task NavigateToRegisterAsync()
    {
        HasError = false;
        ErrorMessage = null;
        await Shell.Current.GoToAsync(nameof(RegisterPage));
    }
}
