using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Extensions;
using RaceDayAPI.Models;
using RaceDayAPI.Services;

namespace RaceDayAPI.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(AppDbContext db, ITokenService tokens) : ControllerBase
{
    /// <summary>Creates a new user account as either an Organiser or a Participant.</summary>
    /// <remarks>
    /// Expects: fullName, email, password (min 8 characters), role ("Organiser" or "Participant") and an optional phoneNumber.
    /// The password is hashed with BCrypt before it is stored. Returns the new user (never the password).
    /// </remarks>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var roleName = new[] { RoleNames.Organiser, RoleNames.Participant }
            .FirstOrDefault(r => r.Equals(request.Role.Trim(), StringComparison.OrdinalIgnoreCase));

        if (roleName is null)
        {
            ModelState.AddModelError(nameof(request.Role), "Role must be 'Organiser' or 'Participant'.");
            return ValidationProblem(ModelState);
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email))
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "That email address is already registered.");

        var role = await db.Roles.FirstAsync(r => r.RoleName == roleName);

        var user = new AppUser
        {
            RoleID = role.RoleID,
            Role = role,
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            PhoneNumber = request.PhoneNumber?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Created("/api/users/me", user.ToDto());
    }

    /// <summary>Authenticates a user and issues a JWT access token.</summary>
    /// <remarks>
    /// Expects: email and password. Returns a token plus a user summary. Send the token as
    /// "Authorization: Bearer {token}" on every later request (use the Authorize button in Swagger).
    /// </remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid email or password.");

        var (token, expiresAt) = tokens.CreateToken(user);
        return Ok(new AuthResponse(token, expiresAt, user.ToDto()));
    }
}
