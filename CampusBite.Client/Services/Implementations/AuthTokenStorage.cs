using CampusBite.Services.Interfaces;
using CampusBite.Shared.Enums;
using Microsoft.Maui.Storage;

namespace CampusBite.Services.Implementations;

public class AuthTokenStorage : IAuthTokenStorage
{
    private const string AccessTokenKey = "campusbite_access_token";
    private const string RefreshTokenKey = "campusbite_refresh_token";
    private const string UserIdKey = "campusbite_user_id";
    private const string UserEmailKey = "campusbite_user_email";
    private const string UserFullNameKey = "campusbite_user_fullname";
    private const string UserRoleKey = "campusbite_user_role";
    private const string CafeIdKey = "campusbite_cafe_id";

    public async Task SaveAuthDataAsync(
        string accessToken,
        string refreshToken,
        int userId,
        string email,
        string fullName,
        UserRole role,
        int? cafeId)
    {
        await SecureStorage.Default.SetAsync(AccessTokenKey, accessToken);
        await SecureStorage.Default.SetAsync(RefreshTokenKey, refreshToken);

        Preferences.Default.Set(UserIdKey, userId);
        Preferences.Default.Set(UserEmailKey, email);
        Preferences.Default.Set(UserFullNameKey, fullName);
        Preferences.Default.Set(UserRoleKey, (int)role);

        if (cafeId.HasValue)
        {
            Preferences.Default.Set(CafeIdKey, cafeId.Value);
        }
        else
        {
            Preferences.Default.Remove(CafeIdKey);
        }
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        return await SecureStorage.Default.GetAsync(AccessTokenKey);
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        return await SecureStorage.Default.GetAsync(RefreshTokenKey);
    }

    public Task<int?> GetUserIdAsync()
    {
        if (Preferences.Default.ContainsKey(UserIdKey))
        {
            return Task.FromResult<int?>(Preferences.Default.Get(UserIdKey, 0));
        }
        return Task.FromResult<int?>(null);
    }

    public Task<string?> GetUserEmailAsync()
    {
        return Task.FromResult(Preferences.Default.Get<string?>(UserEmailKey, null));
    }

    public Task<string?> GetUserFullNameAsync()
    {
        return Task.FromResult(Preferences.Default.Get<string?>(UserFullNameKey, null));
    }

    public Task<UserRole?> GetUserRoleAsync()
    {
        if (Preferences.Default.ContainsKey(UserRoleKey))
        {
            var rawRole = Preferences.Default.Get(UserRoleKey, (int)UserRole.Student);
            return Task.FromResult<UserRole?>((UserRole)rawRole);
        }
        return Task.FromResult<UserRole?>(null);
    }

    public Task<int?> GetCafeIdAsync()
    {
        if (Preferences.Default.ContainsKey(CafeIdKey))
        {
            return Task.FromResult<int?>(Preferences.Default.Get(CafeIdKey, 0));
        }
        return Task.FromResult<int?>(null);
    }

    public async Task UpdateTokensAsync(string accessToken, string refreshToken)
    {
        await SecureStorage.Default.SetAsync(AccessTokenKey, accessToken);
        await SecureStorage.Default.SetAsync(RefreshTokenKey, refreshToken);
    }

    public Task ClearAuthDataAsync()
    {
        SecureStorage.Default.Remove(AccessTokenKey);
        SecureStorage.Default.Remove(RefreshTokenKey);

        Preferences.Default.Remove(UserIdKey);
        Preferences.Default.Remove(UserEmailKey);
        Preferences.Default.Remove(UserFullNameKey);
        Preferences.Default.Remove(UserRoleKey);
        Preferences.Default.Remove(CafeIdKey);

        return Task.CompletedTask;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetAccessTokenAsync();
        return !string.IsNullOrWhiteSpace(token);
    }
}
