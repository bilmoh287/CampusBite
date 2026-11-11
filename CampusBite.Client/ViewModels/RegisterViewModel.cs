using System.Collections.ObjectModel;
using CampusBite.Services.Interfaces;
using CampusBite.Services.Network;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Enums;
using CampusBite.Shared.Requests;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CampusBite.ViewModels;

public partial class RegisterViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly IApiClient _apiClient;

    public ObservableCollection<UniversityDto> Universities { get; } = new();

    public List<string> Roles { get; } = new() { "Student", "Courier", "Café Owner" };

    [ObservableProperty]
    private string _fullName = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _phoneNumber = string.Empty;

    [ObservableProperty]
    private string _selectedRole = "Student";

    [ObservableProperty]
    private UniversityDto? _selectedUniversity;

    [ObservableProperty]
    private bool _isStudent = true;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    public RegisterViewModel(IAuthService authService, IApiClient apiClient)
    {
        _authService = authService;
        _apiClient = apiClient;
    }

    partial void OnSelectedRoleChanged(string value)
    {
        IsStudent = value == "Student";
    }

    public async Task InitializeAsync()
    {
        if (Universities.Count > 0) return;

        try
        {
            var response = await _apiClient.GetAsync<List<UniversityDto>>(ApiConstants.Universities);
            if (response != null && response.Success && response.Data != null)
            {
                Universities.Clear();
                foreach (var uni in response.Data)
                {
                    Universities.Add(uni);
                }

                if (Universities.Count > 0)
                {
                    SelectedUniversity = Universities[0];
                }
            }
        }
        catch
        {
            // Fallback gracefully if API is temporarily unreachable during registration setup
        }
    }

    [RelayCommand]
    public async Task RegisterAsync()
    {
        if (IsBusy) return;

        if (string.IsNullOrWhiteSpace(FullName) || FullName.Trim().Length < 2)
        {
            ErrorMessage = "Please enter your full name (at least 2 characters).";
            HasError = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@'))
        {
            ErrorMessage = "Please enter a valid email address.";
            HasError = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6)
        {
            ErrorMessage = "Password must be at least 6 characters long.";
            HasError = true;
            return;
        }

        var role = SelectedRole switch
        {
            "Courier" => UserRole.Courier,
            "Café Owner" => UserRole.CafeOwner,
            _ => UserRole.Student
        };

        if (role == UserRole.Student && SelectedUniversity == null && Universities.Count > 0)
        {
            ErrorMessage = "Please select your university campus.";
            HasError = true;
            return;
        }

        try
        {
            IsBusy = true;
            HasError = false;
            ErrorMessage = null;

            var request = new RegisterRequest
            {
                FullName = FullName.Trim(),
                Email = Email.Trim(),
                Password = Password,
                PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim(),
                Role = role,
                UniversityId = role == UserRole.Student ? SelectedUniversity?.Id : null
            };

            var response = await _authService.RegisterAsync(request);

            if (response != null && response.Success && response.Data != null)
            {
                var registeredRole = response.Data.User.Role;

                if (Shell.Current is AppShell appShell)
                {
                    appShell.ApplyRoleGating(registeredRole);
                }

                switch (registeredRole)
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
                ErrorMessage = response?.Message ?? "Registration failed. Please check your information.";
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
    public async Task NavigateToLoginAsync()
    {
        HasError = false;
        ErrorMessage = null;
        await Shell.Current.GoToAsync("..");
    }
}
