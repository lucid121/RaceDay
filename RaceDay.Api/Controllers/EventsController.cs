using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayDbContext _db;

        public EventsController(RaceDayDbContext db)
        {
            _db = db;
        }

        // 3. GET /api/events (Any logged-in user)
        [HttpGet]
        [SessionAuth] // No RoleId required, just login
        public async Task<IActionResult> GetEvents([FromQuery] DateTime? date, [FromQuery] string? location)
        {
            // Start with upcoming events only
            var query = _db.Events.Where(e => e.EventDate >= DateTime.Today).AsQueryable();

            // Optional filters
            if (date.HasValue)
            {
                query = query.Where(e => e.EventDate == date.Value);
            }
            if (!string.IsNullOrEmpty(location))
            {
                query = query.Where(e => e.Location.Contains(location));
            }

            var events = await query.ToListAsync();

            if (events.Count == 0)
            {
                return NoContent(); // 204
            }

            return Ok(events);
        }

        // 4. POST /api/events (Organiser only)
        [HttpPost]
        [SessionAuth(RequiredRoleId = 1)] // 1 = Organiser
        public async Task<IActionResult> CreateEvent([FromBody] Event newEvent)
        {
            // Get OrganiserId from session (Filter ensures user is logged in)
            var organiserId = HttpContext.Session.GetInt32("UserId").Value;

            newEvent.OrganiserId = organiserId;
            newEvent.EventId = 0; // Ensure EF creates a new one

            if (newEvent.EventDate < DateTime.Today)
            {
                return BadRequest("Event date must be in the future.");
            }

            _db.Events.Add(newEvent);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEvents), new { id = newEvent.EventId }, newEvent);
        }

        // 5. PUT /api/events/{id} (Organiser only)
        [HttpPut("{id}")]
        [SessionAuth(RequiredRoleId = 1)]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] Event updatedEvent)
        {
            var organiserId = HttpContext.Session.GetInt32("UserId").Value;

            var existingEvent = await _db.Events.FindAsync(id);
            if (existingEvent == null) return NotFound(); // 404

            // Check ownership: Is this the organiser's event?
            if (existingEvent.OrganiserId != organiserId)
            {
                return Forbid(); // 403
            }

            // Update fields
            existingEvent.EventName = updatedEvent.EventName;
            existingEvent.EventDate = updatedEvent.EventDate;
            existingEvent.Location = updatedEvent.Location;
            existingEvent.Description = updatedEvent.Description;

            await _db.SaveChangesAsync();
            return NoContent(); // 204
        }

        // 6. DELETE /api/events/{id} (Organiser only)
        [HttpDelete("{id}")]
        [SessionAuth(RequiredRoleId = 1)]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var organiserId = HttpContext.Session.GetInt32("UserId").Value;

            var existingEvent = await _db.Events.FindAsync(id);
            if (existingEvent == null) return NotFound(); // 404

            // Check ownership
            if (existingEvent.OrganiserId != organiserId)
            {
                return Forbid(); // 403
            }

            _db.Events.Remove(existingEvent);
            await _db.SaveChangesAsync();
            return NoContent(); // 204
        }
    }
}