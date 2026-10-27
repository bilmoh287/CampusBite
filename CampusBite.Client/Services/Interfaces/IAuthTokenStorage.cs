using CampusBite.Shared.Enums;

namespace CampusBite.Services.Interfaces;

public interface IAuthTokenStorage
{
    Task SaveAuthDataAsync(
        string accessToken,
        string refreshToken,
        int userId,
        string email,
        string fullName,
        UserRole role,
        int? cafeId);

    Task<string?> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    Task<int?> GetUserIdAsync();
    Task<string?> GetUserEmailAsync();
    Task<string?> GetUserFullNameAsync();
    Task<UserRole?> GetUserRoleAsync();
    Task<int?> GetCafeIdAsync();

    Task UpdateTokensAsync(string accessToken, string refreshToken);
    Task ClearAuthDataAsync();
    Task<bool> IsAuthenticatedAsync();
}
