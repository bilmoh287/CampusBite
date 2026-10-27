using CampusBite.Shared.DTOs;
using CampusBite.Shared.Requests;
using CampusBite.Shared.Responses;

namespace CampusBite.Services.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<AuthResponse>?> LoginAsync(string email, string password);
    Task<ApiResponse<AuthResponse>?> RegisterAsync(RegisterRequest request);
    Task LogoutAsync();
    Task<ApiResponse<UserDto>?> GetCurrentUserAsync();
    Task<bool> IsAuthenticatedAsync();
}
