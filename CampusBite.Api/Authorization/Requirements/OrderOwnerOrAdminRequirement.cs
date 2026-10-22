using Microsoft.AspNetCore.Authorization;

namespace CampusBite.Api.Authorization.Requirements;

/// <summary>
/// Defines the rule: Only the Student who ordered, the Café Owner preparing it,
/// the Courier delivering it, or a System Admin can access this order ticket.
/// </summary>
public class OrderOwnerOrAdminRequirement : IAuthorizationRequirement
{
}
