using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Race results. Organisers capture them for their own events. Participants view their own.
/// </summary>
[ApiController]
[SessionAuthorize] // default: any logged-in user
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db)
    {
        _db = db;
    }

    // One recipe to turn a Result into a ResultDto
    private static readonly Expression<Func<Result, ResultDto>> ToDto = r => new ResultDto
    {
        ResultId = r.ResultId,
        EnrolmentId = r.EnrolmentId,
        ParticipantName = r.Enrolment.User.Name + " " + r.Enrolment.User.Surname,
        EventId = r.Enrolment.Category.EventId,
        EventName = r.Enrolment.Category.Event.EventName,
        EventDate = r.Enrolment.Category.Event.EventDate,
        CategoryName = r.Enrolment.Category.CategoryName,
        FinishTime = r.FinishTime,
        Position = r.Position,
        Remarks = r.Remarks,
        // Count the people in the same category who have a finish time
        TotalFinishers = r.Enrolment.Category.Enrolments
            .Count(x => x.Result != null && x.Result.FinishTime != null)
    };

    /// <summary>
    /// Save the result (finish time and position) for one enrolment (Organiser only).
    /// </summary>
    /// <remarks>
    /// If the enrolment has no result yet, one is created. If it already has one, it is updated.
    /// The enrolment status is set to Completed.
    /// </remarks>
    /// <response code="200">Existing result updated.</response>
    /// <response code="201">New result created.</response>
    /// <response code="400">The finish time or position is not valid.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No enrolment with this id.</response>
    [HttpPut("api/enrolments/{enrolmentId:int}/result")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Save(int enrolmentId, ResultRequestDto dto)
    {
        // The database "Time" type only holds a time inside one day
        var finishTime = dto.FinishTime!.Value;
        if (finishTime <= TimeSpan.Zero || finishTime >= TimeSpan.FromDays(1))
        {
            return BadRequest(new { message = "Finish time must be between 00:00:01 and 23:59:59." });
        }

        // Load the enrolment, its event (to check the owner) and its result (if any)
        var enrolment = await _db.Enrolments
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);
        if (enrolment == null)
        {
            return NotFound(new { message = "Enrolment not found." });
        }

        if (enrolment.Category.Event.OrganiserId != HttpContext.Session.GetCurrentUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only capture results for your own events." });
        }

        var isNew = enrolment.Result == null;
        if (isNew)
        {
            enrolment.Result = new Result { EnrolmentId = enrolmentId };
        }

        enrolment.Result!.FinishTime = finishTime;
        enrolment.Result.Position = dto.Position;
        enrolment.Result.Remarks = dto.Remarks?.Trim();

        // The person has finished, so the enrolment is now Completed
        enrolment.Status = "Completed";
        await _db.SaveChangesAsync();

        var saved = await _db.Results.Where(r => r.EnrolmentId == enrolmentId).Select(ToDto).FirstAsync();
        return isNew ? StatusCode(StatusCodes.Status201Created, saved) : Ok(saved);
    }

    /// <summary>
    /// Get all results of one of my events (Organiser only).
    /// </summary>
    /// <response code="200">The results, ordered by category and position.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No event with this id.</response>
    [HttpGet("api/events/{eventId:int}/results")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByEvent(int eventId)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        if (ev.OrganiserId != HttpContext.Session.GetCurrentUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only view results for your own events." });
        }

        var results = await _db.Results
            .Where(r => r.Enrolment.Category.EventId == eventId)
            .OrderBy(r => r.Enrolment.Category.CategoryName)
            .ThenBy(r => r.Position)
            .Select(ToDto)
            .ToListAsync();

        return Ok(results);
    }

    /// <summary>
    /// Get my own race results (Participant only).
    /// </summary>
    /// <response code="200">My results, newest event first.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not a Participant.</response>
    [HttpGet("api/results/mine")]
    [SessionAuthorize("Participant")]
    [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var userId = HttpContext.Session.GetCurrentUserId();

        var results = await _db.Results
            .Where(r => r.Enrolment.UserId == userId)
            .OrderByDescending(r => r.Enrolment.Category.Event.EventDate)
            .Select(ToDto)
            .ToListAsync();

        return Ok(results);
    }
}