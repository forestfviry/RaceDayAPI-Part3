using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Extensions;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers;

[ApiController]
[Produces("application/json")]
public class EnrolmentsController(AppDbContext db) : ControllerBase
{
    /// <summary>Enrols the logged-in Participant into a race category.</summary>
    /// <remarks>
    /// Expects the category id in the route (no body). The enrolment links the participant, the category and its event.
    /// Returns the enrolment (201). 404 if the category does not exist, 409 if already enrolled or the category is full,
    /// 403 if the caller is an Organiser.
    /// </remarks>
    [HttpPost("api/categories/{categoryId:int}/enrol")]
    [Authorize(Roles = RoleNames.Participant)]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int categoryId)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.CategoryID == categoryId);
        if (category is null) return NotFound();

        var userId = User.GetUserId();

        if (await db.Enrolments.AnyAsync(e => e.CategoryID == categoryId && e.UserID == userId))
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "You are already enrolled in this category.");

        var taken = await db.Enrolments.CountAsync(e => e.CategoryID == categoryId);
        if (taken >= category.MaxParticipants)
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "This category is full.");

        var enrolment = new Enrolment
        {
            UserID = userId,
            CategoryID = categoryId,
            EnrolmentDate = DateTime.UtcNow,
            Status = EnrolmentStatuses.Registered
        };

        db.Enrolments.Add(enrolment);
        await db.SaveChangesAsync();

        var created = await db.Enrolments.AsNoTracking()
            .Include(e => e.User)
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .FirstAsync(e => e.EnrolmentID == enrolment.EnrolmentID);

        return CreatedAtAction(nameof(GetMine), created.ToDto());
    }

    /// <summary>Lists all enrolments belonging to the logged-in Participant.</summary>
    /// <remarks>Expects no body. Returns an array of enrolments including event and category names.</remarks>
    [HttpGet("api/users/me/enrolments")]
    [Authorize(Roles = RoleNames.Participant)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<EnrolmentDto>>> GetMine()
    {
        var userId = User.GetUserId();
        var enrolments = await db.Enrolments.AsNoTracking()
            .Include(e => e.User)
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .Where(e => e.UserID == userId)
            .OrderBy(e => e.EnrolmentDate)
            .ToListAsync();

        return Ok(enrolments.Select(e => e.ToDto()));
    }

    /// <summary>Lists all participants enrolled in a category (Organiser view, own events only).</summary>
    /// <remarks>Expects the category id in the route. Returns an array of enrolments. 403 if not the owning organiser, 404 if not found.</remarks>
    [HttpGet("api/categories/{categoryId:int}/enrolments")]
    [Authorize(Roles = RoleNames.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<EnrolmentDto>>> GetForCategory(int categoryId)
    {
        var category = await db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == categoryId);
        if (category is null) return NotFound();
        if (category.Event.OrganiserID != User.GetUserId()) return Forbid();

        var enrolments = await db.Enrolments.AsNoTracking()
            .Include(e => e.User)
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .Where(e => e.CategoryID == categoryId)
            .OrderBy(e => e.EnrolmentDate)
            .ToListAsync();

        return Ok(enrolments.Select(e => e.ToDto()));
    }

    /// <summary>Cancels the logged-in Participant's own enrolment.</summary>
    /// <remarks>Expects the enrolment id in the route. Returns 204 on success, 403 if it is not your enrolment, 404 if not found.</remarks>
    [HttpDelete("api/enrolments/{id:int}")]
    [Authorize(Roles = RoleNames.Participant)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(int id)
    {
        var enrolment = await db.Enrolments.Include(e => e.Result).FirstOrDefaultAsync(e => e.EnrolmentID == id);
        if (enrolment is null) return NotFound();
        if (enrolment.UserID != User.GetUserId()) return Forbid();

        if (enrolment.Result is not null) db.Results.Remove(enrolment.Result);
        db.Enrolments.Remove(enrolment);

        await db.SaveChangesAsync();
        return NoContent();
    }
}
