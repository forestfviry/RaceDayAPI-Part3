using System.Security.Claims;

namespace RaceDayAPI.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Reads the logged-in user's ID from the JWT.</summary>
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var id) ? id : 0;
    }
}
