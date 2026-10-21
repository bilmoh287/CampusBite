using CampusBite.Api.Domain.Entities;
using CampusBite.Api.Infrastructure.Data;
using CampusBite.Api.Services.Interfaces;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Requests;
using CampusBite.Shared.Responses;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CampusBite.Api.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly CampusBiteDbContext _context;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        CampusBiteDbContext context,
        IJwtTokenGenerator jwtTokenGenerator,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _context = context;
        _jwtTokenGenerator = jwtTokenGenerator;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        // 1. Check if email is already registered
        var emailNormalized = request.Email.Trim().ToLowerInvariant();
        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == emailNormalized))
        {
            return ApiResponse<AuthResponse>.FailureResult("An account with this email address already exists.");
        }

        // 2. Validate University if provided
        string? universityName = null;
        if (request.UniversityId.HasValue)
        {
            var university = await _context.Universities.FindAsync(request.UniversityId.Value);
            if (university == null || !university.IsActive)
            {
                return ApiResponse<AuthResponse>.FailureResult("Selected university was not found or is inactive.");
            }
            universityName = university.Name;
        }

        // 3. Hash Password using BCrypt
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // 4. Create User entity
        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = emailNormalized,
            PasswordHash = passwordHash,
            Role = request.Role,
            PhoneNumber = request.PhoneNumber?.Trim(),
            UniversityId = request.UniversityId,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // 5. Generate Access & Refresh Tokens
        var (accessToken, expiresAt) = _jwtTokenGenerator.GenerateAccessToken(user);
        var refreshTokenString = _jwtTokenGenerator.GenerateRefreshToken();

        var refreshDays = int.TryParse(_configuration["JwtSettings:RefreshTokenExpiryDays"], out var days) ? days : 7;
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenString,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshDays),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        _logger.LogInformation("New user registered successfully: {Email} with role {Role}", user.Email, user.Role);

        return ApiResponse<AuthResponse>.SuccessResult(new AuthResponse
        {
            Token = accessToken,
            RefreshToken = refreshTokenString,
            ExpiresAt = expiresAt,
            User = MapToDto(user, universityName)
        }, "Registration successful.");
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var emailNormalized = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .Include(u => u.University)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == emailNormalized);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
            return ApiResponse<AuthResponse>.FailureResult("Invalid email or password.");
        }

        // Generate Access & Refresh Tokens
        var (accessToken, expiresAt) = _jwtTokenGenerator.GenerateAccessToken(user);
        var refreshTokenString = _jwtTokenGenerator.GenerateRefreshToken();

        var refreshDays = int.TryParse(_configuration["JwtSettings:RefreshTokenExpiryDays"], out var days) ? days : 7;
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenString,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshDays),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User logged in successfully: {Email}", user.Email);

        return ApiResponse<AuthResponse>.SuccessResult(new AuthResponse
        {
            Token = accessToken,
            RefreshToken = refreshTokenString,
            ExpiresAt = expiresAt,
            User = MapToDto(user, user.University?.Name)
        }, "Login successful.");
    }

    public async Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        // 1. Get principal from expired token to read Sub claim
        ClaimsPrincipal? principal;
        try
        {
            principal = _jwtTokenGenerator.GetPrincipalFromExpiredToken(request.Token);
            if (principal == null)
            {
                return ApiResponse<AuthResponse>.FailureResult("Invalid access token.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed during refresh token request.");
            return ApiResponse<AuthResponse>.FailureResult("Invalid access token.");
        }

        var userIdString = principal.FindFirstValue(ClaimTypes.NameIdentifier) 
            ?? principal.FindFirstValue("sub");

        if (!int.TryParse(userIdString, out var userId))
        {
            return ApiResponse<AuthResponse>.FailureResult("Invalid token claims.");
        }

        // 2. Find matching active refresh token in database
        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .ThenInclude(u => u.University)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && rt.UserId == userId);

        if (storedToken == null || !storedToken.IsActive)
        {
            _logger.LogWarning("Invalid or expired refresh token attempted for user ID: {UserId}", userId);
            return ApiResponse<AuthResponse>.FailureResult("Invalid or expired refresh token.");
        }

        // 3. Token Rotation (Revoke old token, issue new token pair)
        var newRefreshTokenString = _jwtTokenGenerator.GenerateRefreshToken();
        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.ReplacedByToken = newRefreshTokenString;

        var refreshDays = int.TryParse(_configuration["JwtSettings:RefreshTokenExpiryDays"], out var days) ? days : 7;
        var newRefreshToken = new RefreshToken
        {
            UserId = userId,
            Token = newRefreshTokenString,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshDays),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        await _context.RefreshTokens.AddAsync(newRefreshToken);
        await _context.SaveChangesAsync();

        // 4. Generate new access token
        var (newAccessToken, expiresAt) = _jwtTokenGenerator.GenerateAccessToken(storedToken.User);

        _logger.LogInformation("Tokens successfully refreshed for user ID: {UserId}", userId);

        return ApiResponse<AuthResponse>.SuccessResult(new AuthResponse
        {
            Token = newAccessToken,
            RefreshToken = newRefreshTokenString,
            ExpiresAt = expiresAt,
            User = MapToDto(storedToken.User, storedToken.User.University?.Name)
        }, "Token refreshed successfully.");
    }

    public async Task<ApiResponse<bool>> RevokeTokenAsync(string refreshToken)
    {
        var token = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);
        if (token == null || token.IsRevoked)
        {
            return ApiResponse<bool>.FailureResult("Refresh token is not found or already revoked.");
        }

        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Refresh token revoked for user ID: {UserId}", token.UserId);
        return ApiResponse<bool>.SuccessResult(true, "Token revoked successfully (Logged out).");
    }

    public async Task<ApiResponse<UserDto>> GetCurrentUserAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.University)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return ApiResponse<UserDto>.FailureResult("User not found.");
        }

        return ApiResponse<UserDto>.SuccessResult(MapToDto(user, user.University?.Name));
    }

    private static UserDto MapToDto(User user, string? universityName)
    {
        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            PhoneNumber = user.PhoneNumber,
            UniversityId = user.UniversityId,
            UniversityName = universityName,
            CreatedAt = user.CreatedAt
        };
    }
}
