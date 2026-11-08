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
/// Categories inside an event (for example "10km Run"). Everyone logged in can view.
/// Only the Organiser who owns the event can add, change or delete them.
/// </summary>
[ApiController]
[SessionAuthorize] // default: any logged-in user
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db)
    {
        _db = db;
    }

    // One recipe to turn a Category into a CategoryDto
    private static readonly Expression<Func<Category, CategoryDto>> ToDto = c => new CategoryDto
    {
        CategoryId = c.CategoryId,
        EventId = c.EventId,
        CategoryName = c.CategoryName,
        Distance = c.Distance,
        EntryFee = c.EntryFee
    };

    /// <summary>
    /// Get all categories of one event (Organiser and Participant).
    /// </summary>
    /// <response code="200">The list of categories.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="404">No event with this id.</response>
    [HttpGet("api/events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByEvent(int eventId)
    {
        var eventExists = await _db.Events.AnyAsync(e => e.EventId == eventId);
        if (!eventExists)
        {
            return NotFound(new { message = "Event not found." });
        }

        var categories = await _db.Categories
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.Distance)
            .Select(ToDto)
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>
    /// Add a category to one of my events (Organiser only).
    /// </summary>
    /// <response code="201">Category created.</response>
    /// <response code="400">Some of the data is not valid.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No event with this id.</response>
    [HttpPost("api/events/{eventId:int}/categories")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int eventId, CategoryRequestDto dto)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        // Only the owner of the event can add categories
        if (ev.OrganiserId != HttpContext.Session.GetCurrentUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only add categories to your own events." });
        }

        var category = new Category
        {
            EventId = eventId,
            CategoryName = dto.CategoryName.Trim(),
            Distance = dto.Distance,
            EntryFee = dto.EntryFee
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        var created = await _db.Categories.Where(c => c.CategoryId == category.CategoryId).Select(ToDto).FirstAsync();
        return StatusCode(StatusCodes.Status201Created, created);
    }

    /// <summary>
    /// Update a category (Organiser only, and only in my own events).
    /// </summary>
    /// <response code="200">Category updated.</response>
    /// <response code="400">Some of the data is not valid.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No category with this id.</response>
    [HttpPut("api/categories/{id:int}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, CategoryRequestDto dto)
    {
        // Include(c => c.Event) loads the event too, so we can check the owner
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null)
        {
            return NotFound(new { message = "Category not found." });
        }

        if (category.Event.OrganiserId != HttpContext.Session.GetCurrentUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only change categories in your own events." });
        }

        category.CategoryName = dto.CategoryName.Trim();
        category.Distance = dto.Distance;
        category.EntryFee = dto.EntryFee;
        await _db.SaveChangesAsync();

        var updated = await _db.Categories.Where(c => c.CategoryId == id).Select(ToDto).FirstAsync();
        return Ok(updated);
    }

    /// <summary>
    /// Delete a category (Organiser only, and only in my own events).
    /// </summary>
    /// <remarks>This also deletes the enrolments and results of this category.</remarks>
    /// <response code="204">Category deleted.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No category with this id.</response>
    [HttpDelete("api/categories/{id:int}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null)
        {
            return NotFound(new { message = "Category not found." });
        }

        if (category.Event.OrganiserId != HttpContext.Session.GetCurrentUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only delete categories in your own events." });
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
