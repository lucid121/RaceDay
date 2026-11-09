using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Helpers;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Image uploads (event banner and profile picture). Files are saved in Azure Blob Storage.
/// </summary>
[ApiController]
[SessionAuthorize] // default: any logged-in user
public class ImagesController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IBlobStorageService _blobStorage;

    public ImagesController(RaceDayDbContext db, IBlobStorageService blobStorage)
    {
        _db = db;
        _blobStorage = blobStorage;
    }

    /// <summary>
    /// Upload a banner image for one of my events (Organiser only, and only my own events).
    /// </summary>
    /// <remarks>Send the image as form-data with the field name "file". JPG, PNG or WEBP, max 5 MB.</remarks>
    /// <response code="200">Image saved. The image URL is returned.</response>
    /// <response code="400">The file is missing, too big, or not an allowed type.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="403">You are not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No event with this id.</response>
    /// <response code="503">Image storage is not available right now.</response>
    [HttpPost("api/events/{eventId:int}/banner")]
    [SessionAuthorize("Organiser")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UploadEventBanner(int eventId, IFormFile file)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        if (ev.OrganiserId != HttpContext.Session.GetCurrentUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only change the banner of your own events." });
        }

        var error = BlobStorageService.Validate(file);
        if (error != null)
        {
            return BadRequest(new { message = error });
        }

        try
        {
            ev.BannerImageUrl = await _blobStorage.UploadImageAsync(file, "banners");
            await _db.SaveChangesAsync();
            return Ok(new { imageUrl = ev.BannerImageUrl });
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is RequestFailedException)
        {
            // Do not show raw Azure errors to the user
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Image storage is not available right now. Please try again later." });
        }
    }

    /// <summary>
    /// Upload my own profile picture (Organiser and Participant).
    /// </summary>
    /// <remarks>Send the image as form-data with the field name "file". JPG, PNG or WEBP, max 5 MB.</remarks>
    /// <response code="200">Image saved. The image URL is returned.</response>
    /// <response code="400">The file is missing, too big, or not an allowed type.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="503">Image storage is not available right now.</response>
    [HttpPost("api/users/me/picture")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        var error = BlobStorageService.Validate(file);
        if (error != null)
        {
            return BadRequest(new { message = error });
        }

        var userId = HttpContext.Session.GetCurrentUserId();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
        {
            return Unauthorized(new { message = "Please log in first." });
        }

        try
        {
            user.ProfilePictureUrl = await _blobStorage.UploadImageAsync(file, "profiles");
            await _db.SaveChangesAsync();
            return Ok(new { imageUrl = user.ProfilePictureUrl });
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is RequestFailedException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Image storage is not available right now. Please try again later." });
        }
    }
}
