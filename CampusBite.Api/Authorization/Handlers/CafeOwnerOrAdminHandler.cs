using CampusBite.Api.Authorization.Requirements;
using CampusBite.Api.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CampusBite.Api.Authorization.Handlers;

public class CafeOwnerOrAdminHandler : AuthorizationHandler<CafeOwnerOrAdminRequirement, Cafe>
{
    private readonly ILogger<CafeOwnerOrAdminHandler> _logger;

    public CafeOwnerOrAdminHandler(ILogger<CafeOwnerOrAdminHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CafeOwnerOrAdminRequirement requirement,
        Cafe cafe)
    {
        // 1. Admin Override (Full System Access)
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // 2. Extract Authenticated User ID from JWT claims
        var userIdString = context.User.FindFirstValue(ClaimTypes.NameIdentifier) 
            ?? context.User.FindFirstValue("sub");

        if (!int.TryParse(userIdString, out var currentUserId))
        {
            return Task.CompletedTask;
        }

        // 3. Ownership Verification
        if (cafe.OwnerId == currentUserId)
        {
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "Security Alert: User {UserId} attempted unauthorized access to Cafe {CafeId} (Owner: {OwnerId})",
                currentUserId, cafe.Id, cafe.OwnerId);
        }

        return Task.CompletedTask;
    }
}
