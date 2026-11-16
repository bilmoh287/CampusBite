using CampusBite.Api.Services.Interfaces;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusBite.Api.Controllers;

[Authorize]
public class BuddiesController : BaseApiController
{
    private readonly IBuddyService _buddyService;

    public BuddiesController(IBuddyService buddyService)
    {
        _buddyService = buddyService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<BuddyDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBuddies()
    {
        if (!CurrentUserId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _buddyService.GetBuddiesAsync(CurrentUserId.Value);
        return HandleResult(result);
    }

    [HttpPost("{username}")]
    [ProducesResponseType(typeof(ApiResponse<BuddyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BuddyDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddBuddy(string username)
    {
        if (!CurrentUserId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _buddyService.AddBuddyAsync(CurrentUserId.Value, username);
        return HandleResult(result);
    }

    [HttpDelete("{buddyId}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveBuddy(int buddyId)
    {
        if (!CurrentUserId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _buddyService.RemoveBuddyAsync(CurrentUserId.Value, buddyId);
        return HandleResult(result);
    }
}
