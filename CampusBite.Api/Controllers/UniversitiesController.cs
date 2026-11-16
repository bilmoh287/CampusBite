using CampusBite.Api.Services.Interfaces;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusBite.Api.Controllers;

public class UniversitiesController : BaseApiController
{
    private readonly IUniversityService _universityService;

    public UniversitiesController(IUniversityService universityService)
    {
        _universityService = universityService;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<UniversityDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUniversities()
    {
        var result = await _universityService.GetActiveUniversitiesAsync();
        return HandleResult(result);
    }
}
