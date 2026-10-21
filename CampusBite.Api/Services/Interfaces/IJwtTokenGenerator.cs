using CampusBite.Api.Domain.Entities;
using System.Security.Claims;

namespace CampusBite.Api.Services.Interfaces;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt) GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
