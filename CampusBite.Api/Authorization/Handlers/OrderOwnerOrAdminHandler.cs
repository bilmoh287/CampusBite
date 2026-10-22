using CampusBite.Api.Authorization.Requirements;
using CampusBite.Api.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CampusBite.Api.Authorization.Handlers;

public class OrderOwnerOrAdminHandler : AuthorizationHandler<OrderOwnerOrAdminRequirement, Order>
{
    private readonly ILogger<OrderOwnerOrAdminHandler> _logger;

    public OrderOwnerOrAdminHandler(ILogger<OrderOwnerOrAdminHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrderOwnerOrAdminRequirement requirement,
        Order order)
    {
        // 1. Admin Override
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

        // 3. Ownership / Role Checks:
        // - Student who placed the order
        // - Café manager fulfilling the order
        // - Courier delivering the order
        var isCustomer = order.CustomerId == currentUserId;
        var isCafeOwner = order.Cafe != null && order.Cafe.OwnerId == currentUserId;
        var isCourier = order.CourierId.HasValue && order.CourierId.Value == currentUserId;

        if (isCustomer || isCafeOwner || isCourier)
        {
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "Security Alert: User {UserId} attempted unauthorized access to Order {OrderId} (Customer: {CustomerId})",
                currentUserId, order.Id, order.CustomerId);
        }

        return Task.CompletedTask;
    }
}
