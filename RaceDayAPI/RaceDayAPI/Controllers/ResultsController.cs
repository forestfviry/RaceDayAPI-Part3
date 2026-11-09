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
public class ResultsController(AppDbContext db) : ControllerBase
{
    /// <summary>Captures a finishing result for a participant's enrolment (owning Organiser only).</summary>
    /// <remarks>
    /// Expects: finishTime (hh:mm:ss), position and status (Pending, Finished, DNF, DNS, Disqualified).
    /// Returns the result (201). 403 if not the owning organiser, 404 if the enrolment does not exist,
    /// 409 if the enrolment already has a result (use PUT /api/results/{id} to correct it).
    /// </remarks>
    [HttpPost("api/enrolments/{enrolmentId:int}/result")]
    [Authorize(Roles = RoleNames.Organiser)]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(int enrolmentId, [FromBody] ResultRequest request)
    {
        var enrolment = await db.Enrolments
            .Include(e => e.User)
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentID == enrolmentId);

        if (enrolment is null) return NotFound();
        if (enrolment.Category.Event.OrganiserID != User.GetUserId()) return Forbid();
        if (enrolment.Result is not null)
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "This enrolment already has a result.");

        if (!TryResolveStatus(request, out var status))
            return InvalidStatus(request);

        var result = new Result
        {
            EnrolmentID = enrolmentId,
            Enrolment = enrolment,
            FinishTime = request.FinishTime,
            Position = request.Position,
            Status = status
        };

        db.Results.Add(result);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetForCategory), new { categoryId = enrolment.CategoryID }, result.ToDto());
    }

    /// <summary>Corrects an existing result (owning Organiser only).</summary>
    /// <remarks>Expects: finishTime, position and status. Returns the updated result. 403 if not the owning organiser, 404 if not found.</remarks>
    [HttpPut("api/results/{id:int}")]
    [Authorize(Roles = RoleNames.Organiser)]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] ResultRequest request)
    {
        var result = await db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e.User)
            .Include(r => r.Enrolment).ThenInclude(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(r => r.ResultID == id);

        if (result is null) return NotFound();
        if (result.Enrolment.Category.Event.OrganiserID != User.GetUserId()) return Forbid();

        if (!TryResolveStatus(request, out var status))
            return InvalidStatus(request);

        result.FinishTime = request.FinishTime;
        result.Position = request.Position;
        result.Status = status;
        await db.SaveChangesAsync();

        return Ok(result.ToDto());
    }

    /// <summary>Returns the results / leaderboard for a category, ordered by position. Public.</summary>
    /// <remarks>Expects the category id in the route. Returns an array of results (unplaced results last), or 404 if the category does not exist.</remarks>
    [HttpGet("api/categories/{categoryId:int}/result")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ResultDto>>> GetForCategory(int categoryId)
    {
        if (!await db.Categories.AnyAsync(c => c.CategoryID == categoryId)) return NotFound();

        var results = await db.Results.AsNoTracking()
            .Include(r => r.Enrolment).ThenInclude(e => e.User)
            .Where(r => r.Enrolment.CategoryID == categoryId)
            .ToListAsync();

        var sorted = results
            .OrderBy(r => r.Position ?? int.MaxValue)
            .ThenBy(r => r.FinishTime ?? TimeSpan.MaxValue)
            .Select(r => r.ToDto());

        return Ok(sorted);
    }

    private static bool TryResolveStatus(ResultRequest request, out string status)
    {
        if (string.IsNullOrWhiteSpace(request.Status))
        {
            status = request.FinishTime.HasValue ? ResultStatuses.Finished : ResultStatuses.Pending;
            return true;
        }

        var match = ResultStatuses.All.FirstOrDefault(s => s.Equals(request.Status.Trim(), StringComparison.OrdinalIgnoreCase));
        status = match ?? string.Empty;
        return match is not null;
    }

    private IActionResult InvalidStatus(ResultRequest request)
    {
        ModelState.AddModelError(nameof(request.Status), $"Status must be one of: {string.Join(", ", ResultStatuses.All)}.");
        return ValidationProblem(ModelState);
    }
}
