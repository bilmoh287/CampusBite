using CampusBite.Api.Services.Interfaces;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Requests;
using CampusBite.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusBite.Api.Controllers;

public class CafesController : BaseApiController
{
    private readonly ICafeService _cafeService;

    public CafesController(ICafeService cafeService)
    {
        _cafeService = cafeService;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<CafeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCafes([FromQuery] int? universityId, [FromQuery] string? searchTerm)
    {
        var result = await _cafeService.GetCafesAsync(universityId, searchTerm);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CafeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CafeDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCafeById(int id)
    {
        var result = await _cafeService.GetCafeByIdAsync(id);
        return HandleResult(result);
    }

    [HttpGet("my-cafes")]
    [Authorize(Roles = "CafeOwner,Admin")]
    [ProducesResponseType(typeof(ApiResponse<List<CafeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyCafes()
    {
        if (!CurrentUserId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _cafeService.GetCafesByOwnerAsync(CurrentUserId.Value);
        return HandleResult(result);
    }

    [HttpPatch("{id}/announcement")]
    [Authorize(Roles = "CafeOwner,Admin")]
    [ProducesResponseType(typeof(ApiResponse<CafeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CafeDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateAnnouncement(int id, [FromBody] UpdateCafeAnnouncementRequest request)
    {
        if (!CurrentUserId.HasValue) return Unauthorized();

        var result = await _cafeService.UpdateAnnouncementAsync(id, request.Announcement, CurrentUserId.Value);
        return HandleResult(result);
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "CafeOwner,Admin")]
    [ProducesResponseType(typeof(ApiResponse<CafeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CafeDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ToggleStatus(int id, [FromBody] ToggleCafeStatusRequest request)
    {
        if (!CurrentUserId.HasValue) return Unauthorized();

        var result = await _cafeService.ToggleStatusAsync(id, request.IsOpen, CurrentUserId.Value);
        return HandleResult(result);
    }
}
