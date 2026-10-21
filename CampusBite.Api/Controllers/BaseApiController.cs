using CampusBite.Shared.Responses;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CampusBite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected int? CurrentUserId
    {
        get
        {
            var idString = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return int.TryParse(idString, out var id) ? id : null;
        }
    }

    protected string? CurrentUserEmail => 
        User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");

    protected string? CurrentUserRole => 
        User.FindFirstValue(ClaimTypes.Role);

    protected int? CurrentUserUniversityId
    {
        get
        {
            var uniString = User.FindFirstValue("university_id");
            return int.TryParse(uniString, out var id) ? id : null;
        }
    }

    protected ActionResult HandleResult<T>(ApiResponse<T> result)
    {
        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }
}
