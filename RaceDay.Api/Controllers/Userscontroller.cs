using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Helpers;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Lets a logged-in user (Organiser or Participant) see and change their own profile.
/// </summary>
[ApiController]
[Route("api/users")]
[SessionAuthorize] // every endpoint here needs a logged-in user
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public UsersController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get my own profile.
    /// </summary>
    /// <response code="200">Your profile.</response>
    /// <response code="401">You are not logged in.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyProfile()
    {
        // We take the id from the session, NOT from the URL.
        // So a user can only ever see their own profile.
        var userId = HttpContext.Session.GetCurrentUserId();

        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        return Ok(ToDto(user));
    }

    /// <summary>
    /// Update my own name, surname and email.
    /// </summary>
    /// <response code="200">Profile updated. The new profile is returned.</response>
    /// <response code="400">Some of the data is not valid.</response>
    /// <response code="401">You are not logged in.</response>
    /// <response code="409">Another account already uses this email.</response>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateMyProfile(UpdateProfileDto dto)
    {
        var userId = HttpContext.Session.GetCurrentUserId();

        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        var email = dto.Email.Trim().ToLower();

        // Is the email used by SOMEONE ELSE? (our own email is fine)
        var emailTaken = await _db.Users.AnyAsync(u => u.Email == email && u.UserId != userId);
        if (emailTaken)
        {
            return Conflict(new { message = "This email is already used by another account." });
        }

        // Change only the allowed fields
        user.Name = dto.Name.Trim();
        user.Surname = dto.Surname.Trim();
        user.Email = email;
        await _db.SaveChangesAsync();

        return Ok(ToDto(user));
    }

    // Turns a User (database) into a UserResponseDto (safe to send out)
    private static UserResponseDto ToDto(Models.User user)
    {
        return new UserResponseDto
        {
            UserId = user.UserId,
            Name = user.Name,
            Surname = user.Surname,
            Email = user.Email,
            Role = user.Role.RoleName
        };
    }
}