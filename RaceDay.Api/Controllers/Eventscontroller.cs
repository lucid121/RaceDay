using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;


/// Events. Everyone logged in can view. Only Organisers can create, update and delete.
[ApiController]
[Route("api/events")]
[SessionAuthorize] // default: any logged-in user. Some endpoints below are stricter.
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db)
    {
        _db = db;
    }

    // One recipe for turning an Event into an EventDto.
    // We reuse it in every endpoint so the output is always the same.
    private static readonly Expression<Func<Event, EventDto>> ToDto = e => new EventDto
    {
        EventId = e.EventId,
        OrganiserId = e.OrganiserId,
        OrganiserName = e.Organiser.Name + " " + e.Organiser.Surname,
        EventName = e.EventName,
        EventDate = e.EventDate,
        Location = e.Location,
        Distance = e.Distance,
        EventType = e.EventType,
        Description = e.Description
    };

    /// <summary>
    /// Get all events (Organiser and Participant). You can filter the list.
    /// </summary>
    /// <param name="eventType">Optional. Run, Walk or Cycle.</param>
    /// <param name="upcomingOnly">Optional. true = only events from today onwards.</param>
    /// <response code="200">The list of events, soonest first.</response>
    /// <response code="401">You are not logged in.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll([FromQuery] string? eventType, [FromQuery] bool upcomingOnly = false)
    {
        var query = _db.Events.AsQueryable();

        // Add each filter only if the user asked for it
        if (!string.IsNullOrWhiteSpace(eventType))
        {
            query = query.Where(e => e.EventType == eventType);
        }

        if (upcomingOnly)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            query = query.Where(e => e.EventDate >= today);
        }

        var events = await query.OrderBy(e => e.EventDate).Select(ToDto).ToListAsync();
        return Ok(events);
    }

    /// <summary>
    /// Get only the events that I created (Organiser only).
    /// </summary>
    /// <response code="200">My events.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not an Organiser.</response>
    [HttpGet("mine")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(List<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var userId = HttpContext.Session.GetCurrentUserId();

        var events = await _db.Events
            .Where(e => e.OrganiserId == userId)
            .OrderBy(e => e.EventDate)
            .Select(ToDto)
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>
    /// Get one event by its id (Organiser and Participant).
    /// </summary>
    /// <response code="200">The event.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="404">No event with this id.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _db.Events.Where(e => e.EventId == id).Select(ToDto).FirstOrDefaultAsync();
        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        return Ok(ev);
    }

    /// <summary>
    /// Create a new event (Organiser only).
    /// </summary>
    /// <remarks>EventType must be Run, Walk or Cycle. The date cannot be in the past.</remarks>
    /// <response code="201">Event created.</response>
    /// <response code="400">Some of the data is not valid.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not an Organiser.</response>
    [HttpPost]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(EventRequestDto dto)
    {
        // A new event must be today or in the future
        if (dto.EventDate < DateOnly.FromDateTime(DateTime.Today))
        {
            return BadRequest(new { message = "Event date cannot be in the past." });
        }

        var ev = new Event
        {
            // The organiser is whoever is logged in. The user cannot choose this.
            OrganiserId = HttpContext.Session.GetCurrentUserId(),
            EventName = dto.EventName.Trim(),
            EventDate = dto.EventDate,
            Location = dto.Location.Trim(),
            Distance = dto.Distance,
            EventType = dto.EventType,
            Description = dto.Description?.Trim()
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        // Read it back so we return the full EventDto (with organiser name)
        var created = await _db.Events.Where(e => e.EventId == ev.EventId).Select(ToDto).FirstAsync();
        return CreatedAtAction(nameof(GetById), new { id = ev.EventId }, created);
    }

    /// <summary>
    /// Update one of my events (Organiser only, and only my own events).
    /// </summary>
    /// <response code="200">Event updated.</response>
    /// <response code="400">Some of the data is not valid.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not an Organiser, or this event belongs to another Organiser.</response>
    /// <response code="404">No event with this id.</response>
    [HttpPut("{id:int}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, EventRequestDto dto)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        // An Organiser may only change their OWN events
        if (ev.OrganiserId != HttpContext.Session.GetCurrentUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only change your own events." });
        }

        ev.EventName = dto.EventName.Trim();
        ev.EventDate = dto.EventDate;
        ev.Location = dto.Location.Trim();
        ev.Distance = dto.Distance;
        ev.EventType = dto.EventType;
        ev.Description = dto.Description?.Trim();
        await _db.SaveChangesAsync();

        var updated = await _db.Events.Where(e => e.EventId == id).Select(ToDto).FirstAsync();
        return Ok(updated);
    }

    /// <summary>
    /// Delete one of my events (Organiser only, and only my own events).
    /// </summary>
    /// <remarks>This also deletes the event's categories, enrolments and results.</remarks>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not an Organiser, or this event belongs to another Organiser.</response>
    /// <response code="404">No event with this id.</response>
    [HttpDelete("{id:int}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        if (ev.OrganiserId != HttpContext.Session.GetCurrentUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only delete your own events." });
        }

        // The database "cascade" rule removes the categories, enrolments and results too
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}