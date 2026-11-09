using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Extensions;

namespace RaceDayAPI.Controllers;

[ApiController]
[Route("api/users/me")]
[Authorize]
[Produces("application/json")]
public class UsersController(AppDbContext db) : ControllerBase
{
    /// <summary>Returns the profile of the currently logged-in user (any role).</summary>
    /// <remarks>Expects no body. Returns the user object (no password).</remarks>
    [HttpGet]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        var userId = User.GetUserId();
        var user = await db.Users.AsNoTracking().Include(u => u.Role).FirstOrDefaultAsync(u => u.UserID == userId);
        if (user is null) return Unauthorized();
        return Ok(user.ToDto());
    }

    /// <summary>Updates the logged-in user's own profile details (any role).</summary>
    /// <remarks>Expects: fullName and an optional phoneNumber. Returns the updated user.</remarks>
    [HttpPut]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var userId = User.GetUserId();
        var user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserID == userId);
        if (user is null) return Unauthorized();

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();
        await db.SaveChangesAsync();

        return Ok(user.ToDto());
    }
}
