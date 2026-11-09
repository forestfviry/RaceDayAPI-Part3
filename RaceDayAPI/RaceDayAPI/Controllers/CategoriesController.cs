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
public class CategoriesController(AppDbContext db) : ControllerBase
{
    /// <summary>Lists all race categories for a specific event, shortest distance first. Public.</summary>
    /// <remarks>Expects the event id in the route. Returns an array of categories, or 404 if the event does not exist.</remarks>
    [HttpGet("api/events/{eventId:int}/categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetByEvent(int eventId)
    {
        if (!await db.Events.AnyAsync(e => e.EventID == eventId)) return NotFound();

        var categories = await db.Categories.AsNoTracking()
            .Where(c => c.EventID == eventId)
            .OrderBy(c => c.DistanceKm)
            .ToListAsync();

        return Ok(categories.Select(c => c.ToDto()));
    }

    /// <summary>Adds a new category (e.g. 5km, 10km) to an event owned by the logged-in Organiser.</summary>
    /// <remarks>
    /// Expects: categoryName, distanceKm, maxParticipants and entryFee. Returns the new category (201).
    /// 403 if you do not own the event, 404 if the event does not exist.
    /// </remarks>
    [HttpPost("api/events/{eventId:int}/categories")]
    [Authorize(Roles = RoleNames.Organiser)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> Create(int eventId, [FromBody] CategoryRequest request)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.EventID == eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserID != User.GetUserId()) return Forbid();

        var category = new Category
        {
            EventID = eventId,
            CategoryName = request.CategoryName.Trim(),
            DistanceKm = request.DistanceKm,
            MaxParticipants = request.MaxParticipants,
            EntryFee = request.EntryFee
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetByEvent), new { eventId }, category.ToDto());
    }

    /// <summary>Updates a category's details. Only the organiser who owns the event may do this.</summary>
    /// <remarks>Expects: categoryName, distanceKm, maxParticipants and entryFee. Returns the updated category.</remarks>
    [HttpPut("api/categories/{id:int}")]
    [Authorize(Roles = RoleNames.Organiser)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> Update(int id, [FromBody] CategoryRequest request)
    {
        var category = await db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category is null) return NotFound();
        if (category.Event.OrganiserID != User.GetUserId()) return Forbid();

        category.CategoryName = request.CategoryName.Trim();
        category.DistanceKm = request.DistanceKm;
        category.MaxParticipants = request.MaxParticipants;
        category.EntryFee = request.EntryFee;
        await db.SaveChangesAsync();

        return Ok(category.ToDto());
    }

    /// <summary>Removes a category (and its enrolments and results) from an event. Only the owning organiser may do this.</summary>
    /// <remarks>Expects the category id in the route. Returns 204 on success, 403 if not the owner, 404 if not found.</remarks>
    [HttpDelete("api/categories/{id:int}")]
    [Authorize(Roles = RoleNames.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await db.Categories
            .Include(c => c.Event)
            .Include(c => c.Enrolments).ThenInclude(en => en.Result)
            .FirstOrDefaultAsync(c => c.CategoryID == id);

        if (category is null) return NotFound();
        if (category.Event.OrganiserID != User.GetUserId()) return Forbid();

        var enrolments = category.Enrolments.ToList();
        db.Results.RemoveRange(enrolments.Where(en => en.Result is not null).Select(en => en.Result!).ToList());
        db.Enrolments.RemoveRange(enrolments);
        db.Categories.Remove(category);

        await db.SaveChangesAsync();
        return NoContent();
    }
}
