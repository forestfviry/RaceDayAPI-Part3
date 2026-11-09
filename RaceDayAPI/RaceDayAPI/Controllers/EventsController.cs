using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Extensions;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers;

[ApiController]
[Route("api/events")]
[Produces("application/json")]
public class EventsController(AppDbContext db) : ControllerBase
{
    /// <summary>Lists all upcoming events (today onwards), soonest first. Public.</summary>
    /// <remarks>Expects no body. Returns an array of events.</remarks>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetUpcoming()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var events = await db.Events.AsNoTracking()
            .Where(e => e.EventDate >= today)
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        return Ok(events.Select(e => e.ToDto()));
    }

    /// <summary>Returns the details of one specific event. Public.</summary>
    /// <remarks>Expects the event id in the route. Returns the event or 404 if it does not exist.</remarks>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> GetById(int id)
    {
        var ev = await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventID == id);
        if (ev is null) return NotFound();
        return Ok(ev.ToDto());
    }

    /// <summary>Creates a new event owned by the logged-in Organiser.</summary>
    /// <remarks>
    /// Expects: eventName, eventDate (yyyy-MM-dd, not in the past), location and an optional description.
    /// Returns the new event (201). Participants receive 403; anonymous callers receive 401.
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = RoleNames.Organiser)]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventDto>> Create([FromBody] EventRequest request)
    {
        var date = request.EventDate!.Value;
        if (date < DateOnly.FromDateTime(DateTime.Today))
        {
            ModelState.AddModelError(nameof(request.EventDate), "Event date cannot be in the past.");
            return ValidationProblem(ModelState);
        }

        var ev = new Event
        {
            OrganiserID = User.GetUserId(),
            EventName = request.EventName.Trim(),
            EventDate = date,
            Location = request.Location.Trim(),
            Description = request.Description?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        db.Events.Add(ev);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = ev.EventID }, ev.ToDto());
    }

    /// <summary>Updates an event owned by the logged-in Organiser.</summary>
    /// <remarks>
    /// Expects: eventName, eventDate, location and an optional description.
    /// Returns the updated event. 403 if you are not the owning organiser, 404 if the event does not exist.
    /// </remarks>
    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.Organiser)]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> Update(int id, [FromBody] EventRequest request)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.EventID == id);
        if (ev is null) return NotFound();
        if (ev.OrganiserID != User.GetUserId()) return Forbid();

        ev.EventName = request.EventName.Trim();
        ev.EventDate = request.EventDate!.Value;
        ev.Location = request.Location.Trim();
        ev.Description = request.Description?.Trim();
        await db.SaveChangesAsync();

        return Ok(ev.ToDto());
    }

    /// <summary>Deletes an event owned by the logged-in Organiser, together with its categories, enrolments and results.</summary>
    /// <remarks>Expects the event id in the route. Returns 204 on success, 403 if not the owner, 404 if not found.</remarks>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await db.Events
            .Include(e => e.Categories).ThenInclude(c => c.Enrolments).ThenInclude(en => en.Result)
            .FirstOrDefaultAsync(e => e.EventID == id);

        if (ev is null) return NotFound();
        if (ev.OrganiserID != User.GetUserId()) return Forbid();

        // The schema has no cascading deletes, so remove the children first.
        foreach (var category in ev.Categories.ToList())
        {
            var enrolments = category.Enrolments.ToList();
            db.Results.RemoveRange(enrolments.Where(en => en.Result is not null).Select(en => en.Result!).ToList());
            db.Enrolments.RemoveRange(enrolments);
        }
        db.Categories.RemoveRange(ev.Categories.ToList());
        db.Events.Remove(ev);

        await db.SaveChangesAsync();
        return NoContent();
    }
}
