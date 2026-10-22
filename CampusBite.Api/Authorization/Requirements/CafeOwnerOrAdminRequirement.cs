using Microsoft.AspNetCore.Authorization;

namespace CampusBite.Api.Authorization.Requirements;

/// <summary>
/// Defines the rule: Only the Café Owner (manager) or a System Admin can access/manage the café resource.
/// </summary>
public class CafeOwnerOrAdminRequirement : IAuthorizationRequirement
{
}
