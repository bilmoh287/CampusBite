using CampusBite.Api.Services.Interfaces;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusBite.Api.Controllers;

[Authorize]
public class UsersController : BaseApiController
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<List<UserSearchResultDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchUsers(
        [FromQuery] string term,
        [FromQuery] int? cafeId = null)
    {
        var result = await _userService.SearchStudentsAsync(
            term, 
            CurrentUserId, 
            CurrentUserUniversityId, 
            cafeId);

        return HandleResult(result);
    }

    [HttpGet("profile")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile()
    {
        if (!CurrentUserId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _userService.GetUserProfileAsync(CurrentUserId.Value);
        return HandleResult(result);
    }
}
