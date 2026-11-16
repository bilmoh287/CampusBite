using CampusBite.Api.Infrastructure.Data;
using CampusBite.Api.Services.Interfaces;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace CampusBite.Api.Services.Implementations;

public class UniversityService : IUniversityService
{
    private readonly CampusBiteDbContext _context;
    private readonly ILogger<UniversityService> _logger;

    public UniversityService(CampusBiteDbContext context, ILogger<UniversityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<List<UniversityDto>>> GetActiveUniversitiesAsync()
    {
        var universities = await _context.Universities
            .AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .Select(u => new UniversityDto
            {
                Id = u.Id,
                Name = u.Name,
                CampusName = u.CampusName,
                City = u.City,
                IsActive = u.IsActive
            })
            .ToListAsync();

        return ApiResponse<List<UniversityDto>>.SuccessResult(universities);
    }
}
