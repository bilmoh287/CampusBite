using CampusBite.Api.Infrastructure.Data;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusBite.Api.Controllers;

public class UniversitiesController : BaseApiController
{
    private readonly CampusBiteDbContext _context;

    public UniversitiesController(CampusBiteDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<UniversityDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUniversities()
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

        return HandleResult(ApiResponse<List<UniversityDto>>.SuccessResult(universities));
    }
}
