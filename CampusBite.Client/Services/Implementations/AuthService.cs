using CampusBite.Services.Interfaces;
using CampusBite.Services.Network;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Requests;
using CampusBite.Shared.Responses;

namespace CampusBite.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly IApiClient _apiClient;
    private readonly IAuthTokenStorage _tokenStorage;

    public AuthService(IApiClient apiClient, IAuthTokenStorage tokenStorage)
    {
        _apiClient = apiClient;
        _tokenStorage = tokenStorage;
    }

    public async Task<ApiResponse<AuthResponse>?> LoginAsync(string email, string password)
    {
        var request = new LoginRequest
        {
            Email = email.Trim(),
            Password = password
        };

        var response = await _apiClient.PostAsync<LoginRequest, AuthResponse>(ApiConstants.AuthLogin, request);

        if (response != null && response.Success && response.Data != null)
        {
            var auth = response.Data;
            await _tokenStorage.SaveAuthDataAsync(
                auth.Token,
                auth.RefreshToken,
                auth.User.Id,
                auth.User.Email,
                auth.User.FullName,
                auth.User.Role,
                null);
        }

        return response;
    }

    public async Task<ApiResponse<AuthResponse>?> RegisterAsync(RegisterRequest request)
    {
        var response = await _apiClient.PostAsync<RegisterRequest, AuthResponse>(ApiConstants.AuthRegister, request);

        if (response != null && response.Success && response.Data != null)
        {
            var auth = response.Data;
            await _tokenStorage.SaveAuthDataAsync(
                auth.Token,
                auth.RefreshToken,
                auth.User.Id,
                auth.User.Email,
                auth.User.FullName,
                auth.User.Role,
                null);
        }

        return response;
    }

    public async Task LogoutAsync()
    {
        var refreshToken = await _tokenStorage.GetRefreshTokenAsync();
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await _apiClient.PostAsync<string, bool>(ApiConstants.AuthRevokeToken, refreshToken);
        }

        await _tokenStorage.ClearAuthDataAsync();
    }

    public async Task<ApiResponse<UserDto>?> GetCurrentUserAsync()
    {
        return await _apiClient.GetAsync<UserDto>(ApiConstants.AuthMe);
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        return await _tokenStorage.IsAuthenticatedAsync();
    }
}
