using System.Security.Claims;

namespace Ledger.API.Helpers;

public static class ClaimsHelper
{
    public static Guid? GetUserId(ClaimsPrincipal? user)
    {
        var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public static string? GetEmail(ClaimsPrincipal? user)
    {
        return user?.FindFirst(ClaimTypes.Email)?.Value;
    }

    public static string? GetRole(ClaimsPrincipal? user)
    {
        return user?.FindFirst(ClaimTypes.Role)?.Value;
    }

    public static bool IsAdmin(ClaimsPrincipal? user)
    {
        var role = GetRole(user);
        return role == "Admin" || role == "SuperAdmin";
    }

    public static bool IsSuperAdmin(ClaimsPrincipal? user)
    {
        return GetRole(user) == "SuperAdmin";
    }
}

