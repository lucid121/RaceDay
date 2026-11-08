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
/// Enrolments = a Participant entering an event category.
/// Participants enter and view their own. Organisers view and manage the ones for their events.
/// </summary>
[ApiController]
[SessionAuthorize] // default: any logged-in user
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db)
    {
        _db = db;
    }

    // One recipe to turn an Enrolment into an EnrolmentDto
    private static readonly Expression<Func<Enrolment, EnrolmentDto>> ToDto = e => new EnrolmentDto
    {
        EnrolmentId = e.EnrolmentId,
        UserId = e.UserId,
        ParticipantName = e.User.Name + " " + e.User.Surname,
        ParticipantEmail = e.User.Email,
        CategoryId = e.CategoryId,
        CategoryName = e.Category.CategoryName,
        EventId = e.Category.EventId,
        EventName = e.Category.Event.EventName,
        EventDate = e.Category.Event.EventDate,
        EnrolmentDate = e.EnrolmentDate,
        Status = e.Status
    };

    /// <summary>
    /// Enter an event by choosing a category (Participant only).
    /// </summary>
    /// <remarks>The new enrolment starts with the status Pending.</remarks>
    /// <response code="201">You are enrolled.</response>
    /// <response code="400">The event date has already passed.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not a Participant.</response>
    /// <response code="404">No category with this id.</response>
    /// <response code="409">You are already enrolled in this category.</response>
    [HttpPost("api/enrolments")]
    [SessionAuthorize("Participant")]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(EnrolmentRequestDto dto)
    {
        var userId = HttpContext.Session.GetCurrentUserId();

        // Step 1: does the category exist? (load its event too)
        var category = await _db.Categories.Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId);
        if (category == null)
        {
            return NotFound(new { message = "Category not found." });
        }

        // Step 2: you cannot enter an event that is already over
        if (category.Event.EventDate < DateOnly.FromDateTime(DateTime.Today))
        {
            return BadRequest(new { message = "This event has already taken place." });
        }

        // Step 3: you cannot enter the same category twice
        var alreadyEnrolled = await _db.Enrolments
            .AnyAsync(e => e.UserId == userId && e.CategoryId == dto.CategoryId);
        if (alreadyEnrolled)
        {
            return Conflict(new { message = "You are already enrolled in this category." });
        }

        // Step 4: save. UserId comes from the session. The status starts as Pending.
        var enrolment = new Enrolment
        {
            UserId = userId,
            CategoryId = dto.CategoryId
        };
        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        var created = await _db.Enrolments.Where(e => e.EnrolmentId == enrolment.EnrolmentId).Select(ToDto).FirstAsync();
        return StatusCode(StatusCodes.Status201Created, created);
    }

    /// <summary>
    /// Get all of my own enrolments (Participant only).
    /// </summary>
    /// <response code="200">My enrolments, soonest event first.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not a Participant.</response>
    [HttpGet("api/enrolments/mine")]
    [SessionAuthorize("Participant")]
    [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var userId = HttpContext.Session.GetCurrentUserId();

        var enrolments = await _db.Enrolments
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.Category.Event.EventDate)
            .Select(ToDto)
            .ToListAsync();

        return Ok(enrolments);
    }

    /// <summary>
    /// Get all enrolments of one of my events (Organiser only).
    /// </summary>
    /// <response code="200">The enrolments for this event.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No event with this id.</response>
    [HttpGet("api/events/{eventId:int}/enrolments")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
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

        // Only the owner can see who entered
        if (ev.OrganiserId != HttpContext.Session.GetCurrentUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only view enrolments for your own events." });
        }

        var enrolments = await _db.Enrolments
            .Where(e => e.Category.EventId == eventId)
            .OrderBy(e => e.EnrolmentDate)
            .Select(ToDto)
            .ToListAsync();

        return Ok(enrolments);
    }

    /// <summary>
    /// Change the status of an enrolment: Pending, Confirmed or Completed (Organiser only).
    /// </summary>
    /// <response code="200">Status updated.</response>
    /// <response code="400">The status is not valid.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No enrolment with this id.</response>
    [HttpPut("api/enrolments/{id:int}/status")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(int id, UpdateEnrolmentStatusDto dto)
    {
        // Load the enrolment, its category and its event, so we can check the owner
        var enrolment = await _db.Enrolments
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (enrolment == null)
        {
            return NotFound(new { message = "Enrolment not found." });
        }

        if (enrolment.Category.Event.OrganiserId != HttpContext.Session.GetCurrentUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only change enrolments for your own events." });
        }

        enrolment.Status = dto.Status;
        await _db.SaveChangesAsync();

        var updated = await _db.Enrolments.Where(e => e.EnrolmentId == id).Select(ToDto).FirstAsync();
        return Ok(updated);
    }
}