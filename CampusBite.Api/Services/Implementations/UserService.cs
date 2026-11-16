using CampusBite.Api.Infrastructure.Data;
using CampusBite.Api.Services.Interfaces;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Enums;
using CampusBite.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace CampusBite.Api.Services.Implementations;

public class UserService : IUserService
{
    private readonly CampusBiteDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(CampusBiteDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<List<UserSearchResultDto>>> SearchStudentsAsync(
        string searchTerm, 
        int? currentUserId, 
        int? universityId, 
        int? cafeId)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Trim().Length < 2)
        {
            return ApiResponse<List<UserSearchResultDto>>.SuccessResult(new List<UserSearchResultDto>());
        }

        var normalizedTerm = searchTerm.Trim().ToLowerInvariant();

        var query = _context.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Student);

        if (currentUserId.HasValue)
        {
            query = query.Where(u => u.Id != currentUserId.Value);
        }

        if (universityId.HasValue)
        {
            query = query.Where(u => u.UniversityId == universityId.Value);
        }

        query = query.Where(u => u.Username.ToLower().Contains(normalizedTerm) || 
                                 u.FullName.ToLower().Contains(normalizedTerm));

        var users = await query
            .Take(10)
            .ToListAsync();

        var results = new List<UserSearchResultDto>();

        foreach (var user in users)
        {
            bool hasPass = false;
            int remainingCredits = 0;

            if (cafeId.HasValue)
            {
                var contract = await _context.StudentContracts
                    .AsNoTracking()
                    .Include(sc => sc.ContractPlan)
                    .Where(sc => sc.StudentId == user.Id && 
                                 sc.ContractPlan.CafeId == cafeId.Value && 
                                 sc.EndDate >= DateTime.UtcNow &&
                                 sc.Status == ContractStatus.Active)
                    .OrderByDescending(sc => sc.EndDate)
                    .FirstOrDefaultAsync();

                if (contract != null && contract.RemainingCredits > 0)
                {
                    hasPass = true;
                    remainingCredits = contract.RemainingCredits;
                }
            }

            results.Add(new UserSearchResultDto
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                HasActivePassAtCafe = hasPass,
                RemainingPassCredits = remainingCredits
            });
        }

        return ApiResponse<List<UserSearchResultDto>>.SuccessResult(results);
    }

    public async Task<ApiResponse<UserDto>> GetUserProfileAsync(int userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.University)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return ApiResponse<UserDto>.FailureResult("User not found.");
        }

        var dto = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            PhoneNumber = user.PhoneNumber,
            UniversityId = user.UniversityId,
            UniversityName = user.University?.Name,
            EmployedAtCafeId = user.EmployedAtCafeId,
            CreatedAt = user.CreatedAt
        };

        return ApiResponse<UserDto>.SuccessResult(dto);
    }
}
